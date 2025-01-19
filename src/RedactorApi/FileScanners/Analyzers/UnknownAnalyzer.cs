using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class UnknownAnalyzer(IAnalyzer analyzer, ILogger<UnknownAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<UnknownAnalyzer> _logger = logger;
    internal override string SupportedExtension => string.Empty;

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream stream)
    {
        using var _ = LogUtils.Create(_logger, nameof(UnknownAnalyzer), nameof(ExtractTextAsync));
        yield return await ValueTask.FromResult(new PageInfo(0, string.Empty));
    }
}
