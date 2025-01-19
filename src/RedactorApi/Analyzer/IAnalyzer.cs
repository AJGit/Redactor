using RedactorApi.Analyzer.Models;

namespace RedactorApi.Analyzer;

public interface IAnalyzer
{
    Task<AnalysisResponse> AnalyzeTextAsync(ReplacementConfig config, int page, CancellationToken cancellationToken);
    Task<AnalysisResponse> AnalyzeTextAsync(ReplacementConfig config, CancellationToken cancellationToken);
}