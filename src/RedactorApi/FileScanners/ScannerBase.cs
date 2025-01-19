using System.Diagnostics;
using DocumentFormat.OpenXml.Wordprocessing;
using RedactorApi.Analyzer;
using RedactorApi.Analyzer.Models;

namespace RedactorApi.FileScanners;

public abstract partial class ScannerBase(IAnalyzer analyzer, ILogger<ScannerBase> logger) : IFileScanner
{
    private readonly ILogger<ScannerBase> _logger = logger;
    private readonly IAnalyzer _analyzer = analyzer;
    private const string ClassName = nameof(ScannerBase);

    [LoggerMessage(LogLevel.Error, "{className} : Error in {functionName}")]
    static partial void LogException(ILogger logger, string functionName, Exception ex, string className = ClassName);

    [LoggerMessage(LogLevel.Debug, "{className} : Empty Page in {fileName} for page {pageNumber}")]
    static partial void LogEmptyPage(ILogger logger, string functionName,string fileName, int pageNumber, string className = ClassName);

    [LoggerMessage(LogLevel.Debug, "Entering {functionName}")]
    static partial void LogEnter(ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Debug, "Exiting {functionName} Runtime: {elapsed}")]
    static partial void LogExit(ILogger logger, string functionName, TimeSpan elapsed);

    public static long Start(ILogger logger, string functionName)
    {
        LogEnter(logger, functionName);
        return Stopwatch.GetTimestamp();
    }
    public static void End(ILogger logger, string functionName, long startTicks)
    {
        LogExit(logger, functionName, Stopwatch.GetElapsedTime(startTicks));
    }

    public async Task<ScanDocumentResults> ScanDocumentAsync(IFormFile file, ReplacementConfig requestConfig, CancellationToken cancellationToken) 
    {
        const int maxConcurrentTasks = 1;

        const string functionName = nameof(ScanDocumentAsync);
        var pageAnalysis = new List<PageAnalysis>();
        var pageCounter = 0;
        var issueCounter = 0;
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(maxConcurrentTasks);

        await foreach (var (pageNumber, text) in ExtractTextAsync(file.OpenReadStream()).WithCancellation(cancellationToken))
        {
            pageCounter++;
            if (string.IsNullOrWhiteSpace(text))
            {
                LogEmptyPage(_logger, functionName, file.FileName, pageNumber);
                continue;
            }

            var requestConfigCapture = new ReplacementConfig(text)
            {
                Threshold = requestConfig.Threshold,
                StartTag = requestConfig.StartTag,
                EndTag = requestConfig.EndTag,
                ReplacementType = requestConfig.ReplacementType,
                Language = requestConfig.Language
            };
            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    var reviewed = await _analyzer.AnalyzeTextAsync(requestConfigCapture, pageNumber, cancellationToken);
                    if (reviewed.Replacements.Length > 0)
                    {
                        var issues = ExtractIssuesFromAnalysis(reviewed);
                        Interlocked.Add(ref issueCounter, issues.Count);

                        var output = GenerateIssueList(issues);
                        lock (pageAnalysis)
                        {
                            pageAnalysis.Add(new PageAnalysis(reviewed.PageNumber, output));
                        }
                    }
                }
                catch (Exception e)
                {
                    LogException(_logger, functionName, e);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
        var sorted = pageAnalysis.OrderBy(p => p.PageNumber);
        return new ScanDocumentResults(new FileAnalysis([.. sorted]), pageCounter, issueCounter);
    }

    internal abstract IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream stream);

    internal abstract string SupportedExtension
    {
        get;
    }
    string IFileScanner.SupportedExtension => SupportedExtension;

    private static Dictionary<string, List<Analysis>> ExtractIssuesFromAnalysis(AnalysisResponse reviewed)
    {
        Dictionary<string, List<Analysis>> issues = new(StringComparer.OrdinalIgnoreCase);
        foreach (var replacement in reviewed.Replacements)
        {
            var word = reviewed.OriginalText[replacement.Start..replacement.End];
            var exists = issues.TryGetValue(word, out var issueList);
            if (!exists)
            {
                issueList = [replacement];
                issues.Add(word, issueList);
            }
            else
            {
                issueList!.Add(replacement);
            }
        }

        return issues;
    }

    private static IEnumerable<Issue> GenerateIssueList(Dictionary<string, List<Analysis>> issues)
    {
        return from issue in issues
               from replacement in issue.Value
               group replacement by new
               {
                   issue.Key,
                   replacement.EntityType,
                   replacement.Score
               } into g
               select new Issue(
                   g.Key.Key,
                   g.Key.EntityType,
                   g.Key.Score,
                   g.Select(r => new Location(r.Start, r.End))
               );
    }
}
