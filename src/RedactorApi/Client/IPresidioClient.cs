using RedactorApi.Analyzer.Models;

namespace RedactorApi.Client;
public interface IPresidioClient
{
    Task<PresidioAnalysisResponse> AnalyzeAsync(PresidioAnalysisRequest request, CancellationToken cancellationToken);
}