using DocumentFormat.OpenXml.Packaging;
using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class WordAnalyzer(IAnalyzer analyzer, ILogger<WordAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<WordAnalyzer> _logger = logger;
    internal override string SupportedExtension => ".docx";

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream docxStream)
    {
        using var _ = LogUtils.Create(_logger, nameof(WordAnalyzer), nameof(ExtractTextAsync));
        using var wordDoc = WordprocessingDocument.Open(docxStream, false);
        var body = wordDoc.MainDocumentPart?.Document.Body;
        if (body == null)
        {
            yield break;
        }

        foreach (var paragraph in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            if (string.IsNullOrWhiteSpace(paragraph.InnerText))
            {
                continue;
            }

            var sb = StringBuilderCache.Acquire();
            sb.AppendLine(paragraph.InnerText);
            yield return await ValueTask.FromResult(new PageInfo(1, sb.GetStringAndRelease()));
        }
    }
}
