using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class TextAnalyzer(IAnalyzer analyzer, ILogger<TextAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<TextAnalyzer> _logger = logger;
    internal override string SupportedExtension => ".txt";

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream stream)
    {
        using var _ = LogUtils.Create(_logger, nameof(TextAnalyzer), nameof(ExtractTextAsync));
        using var streamReader = new StreamReader(stream);
        var lineCounter = 0;
        while (!streamReader.EndOfStream)
        {
            lineCounter++;
            yield return new PageInfo(lineCounter, (await streamReader.ReadLineAsync())!);
        }
    }
}
