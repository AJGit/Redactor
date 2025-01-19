using nietras.SeparatedValues;
using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class CsvAnalyzer(IAnalyzer analyzer, ILogger<CsvAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<CsvAnalyzer> _logger = logger;

    internal override string SupportedExtension => ".csv";
    private const string ScopeName = nameof(CsvAnalyzer) + "::" + nameof(ExtractTextAsync);

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream csvStream)
    {
        using var _ = LogUtils.Create(_logger, nameof(CsvAnalyzer), nameof(ExtractTextAsync));
        using var reader = Sep.Reader().From(csvStream);
        foreach (var row in reader)
        {
            var sb = StringBuilderCache.Acquire();
            for (var i = 0; i < row.ColCount; i++)
            {
                sb.Append(row[i].ToString());
                sb.Append(" ");
            }
            yield return await ValueTask.FromResult(new PageInfo(1, sb.TrimAndRelease()));
        }
    }
}
