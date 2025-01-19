// using System.Net;

using System.Diagnostics;
using RedactorApi.Analyzer.Models;
using RedactorApi.Models;

namespace RedactorApi.Client;

public sealed class PresidioClient(HttpClient httpClient, ILogger<PresidioClient> logger) : IPresidioClient
{
    private readonly HttpClient _client = httpClient;
    private readonly ILogger<PresidioClient> _logger = logger;

    public async Task<PresidioAnalysisResponse> AnalyzeAsync(PresidioAnalysisRequest request, CancellationToken cancellationToken)
    {
        var content = JsonContent.Create(request, RedactorJsonSerializerContext.Default.PresidioAnalysisRequest);

        _logger.LogInformation("Starting to query the analyze endpoint");
        var e = Stopwatch.GetTimestamp();
        var response = await _client.PostAsync("/analyze", content,  cancellationToken);
        _logger.LogInformation("End query the analyze endpoint took {ms}", Stopwatch.GetElapsedTime(e).Milliseconds);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadFromJsonAsync(Models.RedactorJsonSerializerContext.Default.AnalysisArray, cancellationToken);
        return new PresidioAnalysisResponse(responseContent ?? []);
    }
}
