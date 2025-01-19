using Microsoft.AspNetCore.Mvc;
using RedactorApi.Analyzer.Models;
using RedactorApi.Analyzer;
using RedactorApi.Models;
using RedactorApi.Util;
using RedactorApi.FileScanners;

namespace RedactorApi;

public static class Endpoints
{
    public static void MapApplicationEndpoints(this IEndpointRouteBuilder builder)
    {
        var endpoints = builder.MapGroup("/api")
            .WithTags("Redaction");

        endpoints.MapPost("/verify",
                async ([FromBody] ReplacementConfig requestConfig, IAnalyzer analyzer,  ILogger<Program> logger, CancellationToken cancellationToken) =>
                    await VerifyAsync(analyzer, requestConfig, logger, cancellationToken))
            .WithSummary("Verify the content for Personal Identifiable Information")
            .WithDescription("Analyzes string content for Personal Identifiable Information (PII) and returns a Verified flag indicating if safe or not it also includes a detailed review of the findings.")
            .Produces<VerificationResult>()
            .Produces<UnSuccessfulVerificationResult>()
            .ProducesProblem(400)
            .WithName("Content Verification");


        endpoints.MapPost("/review",
                async ([FromBody] ReplacementConfig requestConfig, IAnalyzer analyzer,  ILogger<Program> logger, CancellationToken cancellationToken) =>
                    await ReviewAsync(analyzer, requestConfig, logger, cancellationToken))
            .WithSummary("Review the content for Personal Identifiable Information")
            .WithDescription("Reviews the string content for Personal Identifiable Information (PII) and returns a Verified flag indicating if safe or not it also includes a detailed review of the findings.")
            .Produces<PostReviewResult>()
            .ProducesProblem(400)
            .WithName("Content Review");

        endpoints.MapPost("/files",
                async ([FromForm] FileUploadRequest form, FileAnalyzerFactory fileAnalyzerFactory, ILogger<Program> logger, CancellationToken cancellationToken) =>
                    await FileReviewAsync(form, fileAnalyzerFactory, logger, cancellationToken))
            .WithSummary("Check files for Personal Identifiable Information")
            .WithDescription("Analyzes uploaded files for Personal Identifiable Information (PII) and returns a detailed review of the findings.")
            .Accepts<FileUploadRequest>("multipart/form-data")
            .WithFormOptions()
            .DisableAntiforgery()
            .Produces<FileReviewResult>()
            .ProducesProblem(400)
            .WithName("File Review");
    }

    private static async Task<IResult> VerifyAsync(
        IAnalyzer analyzer, 
        ReplacementConfig requestConfig,
        ILogger<Program> logger, 
        CancellationToken cancellationToken)
    {
        using var _ = LogUtils.Create(logger, nameof(Endpoints), nameof(VerifyAsync));
        if (string.IsNullOrEmpty(requestConfig.Content))
        {
            var result = new UnSuccessfulVerificationResult(false, "Content review unsuccessful.");
            return Results.Json(result, RedactorJsonSerializerContext.Default.UnSuccessfulVerificationResult);
        }

        var reviewed = await analyzer.AnalyzeTextAsync(requestConfig, cancellationToken);
        return CreateReviewResult(requestConfig, reviewed);
    }

    private static async Task<IResult> ReviewAsync(
        IAnalyzer analyzer, 
        ReplacementConfig requestConfig,
        ILogger<Program> logger, 
        CancellationToken cancellationToken)
    {
        {
            using var _ = LogUtils.Create(logger, nameof(Endpoints), nameof(ReviewAsync));
            if (string.IsNullOrEmpty(requestConfig.Content))
            {
                return Results.Problem("Request body is missing", statusCode: StatusCodes.Status400BadRequest);
            }

            var reviewed = await analyzer.AnalyzeTextAsync(requestConfig, cancellationToken);
            var result = new PostReviewResult(
                $"Content reviewed successfully: {requestConfig.Content.Length} characters analyzed, {reviewed.Replacements.Length} replacements found",
                reviewed.Text,
                reviewed.Replacements
            );
            return Results.Json(result, RedactorJsonSerializerContext.Default.PostReviewResult);
        }
    }

    private static async Task<IResult> FileReviewAsync(
        FileUploadRequest form, 
        FileAnalyzerFactory fileAnalyzerFactory,
        ILogger<Program> logger, 
        CancellationToken cancellationToken)
    {
        using var _ = LogUtils.Create(logger, nameof(Endpoints), nameof(FileReviewAsync));

        if (form.File.Count == 0)
        {
            return Results.Problem("No file or files provided", statusCode: StatusCodes.Status400BadRequest);
        }

        var replacementConfig = CreateFormReplacementConfig(form);
        var fileReviews = await CreateFileReviews(form.File, replacementConfig, fileAnalyzerFactory, cancellationToken);
        var result = new FileReviewResult(fileReviews, form.File.Count);
        return Results.Json(result, RedactorJsonSerializerContext.Default.FileReviewResult);
    }

    private static async Task<List<FileReview>> CreateFileReviews(
        IEnumerable<IFormFile> formFiles,
        ReplacementConfig replacementConfig, 
        FileAnalyzerFactory fileAnalyzerFactory,
        CancellationToken cancellationToken)
    {
        var tasks = formFiles.Select(async file =>
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            var analyzer = fileAnalyzerFactory.GetAnalyzer(extension);
            var scanResult = await analyzer.ScanDocumentAsync(file, replacementConfig, cancellationToken);
            var review =
                $"Size: {file.Length} bytes, containing {scanResult.PageCount} pages, {scanResult.IssueCount} issues found.";
            return new FileReview(file.FileName, review, file.Length, scanResult.PageCount, scanResult.IssueCount,
                scanResult.FileAnalysis);
        });

        var fileReviews = await Task.WhenAll(tasks);
        return fileReviews.ToList();
    }


    private static ReplacementConfig CreateFormReplacementConfig(FileUploadRequest form)
    {
        var replacementConfig = new ReplacementConfig(string.Empty)
        {
            StartTag = form.StartTag ?? string.Empty,
            EndTag = form.EndTag ?? string.Empty,
            Language = form.Language ?? "en",
            ReplacementType = form.ReplacementType ?? ReplacementTextType.EntityType,
            Threshold = form.Threshold ?? 0.4f
        };
        return replacementConfig;
    }

    private static IResult CreateReviewResult(ReplacementConfig requestConfig, AnalysisResponse reviewed)
    {
        var result = new VerificationResult(
            reviewed.Replacements.Length != 0,
            $"Content reviewed successfully: {requestConfig.Content.Length} characters analyzed, {reviewed.Replacements.Length} issues found.",
            reviewed.Replacements.OrderByDescending(s => s.Score).Take(20),
            reviewed.Text
        );
        return Results.Json(result, RedactorJsonSerializerContext.Default.VerificationResult);
    }
}
