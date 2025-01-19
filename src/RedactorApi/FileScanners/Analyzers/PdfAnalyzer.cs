using RedactorApi.Analyzer;
using RedactorApi.Util;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace RedactorApi.FileScanners.Analyzers;

public class PdfAnalyzer(IAnalyzer analyzer, ILogger<PdfAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<PdfAnalyzer> _logger = logger;

    internal override string SupportedExtension => ".pdf";

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream stream)
    {
        using var _ = LogUtils.Create(_logger, nameof(PdfAnalyzer), nameof(ExtractTextAsync));
        using var document = PdfDocument.Open(stream);
        for (var i = 1; i <= document.NumberOfPages; i++)
        {
            var page = document.GetPage(i);
            var text = GetWords(page);
            yield return await ValueTask.FromResult(new PageInfo(i, text.Trim()));
        }
    }

    private static string GetWords(Page page)
    {
        var letters = page.Letters;

        // 1. Extract words
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        // 2. Segment page
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

        // 3. Postprocessing
        var readingOrder = RenderingReadingOrderDetector.Instance;
        var orderedTextBlocks = readingOrder.Get(textBlocks);
        var reOrderedTextBlocks = orderedTextBlocks.OrderByDescending(y => y.BoundingBox.TopLeft.Y)
                                                   .ThenBy(x => x.BoundingBox.TopLeft.X).ToList();

        var sb = StringBuilderCache.Acquire();
        foreach (var line in reOrderedTextBlocks.SelectMany(block => block.TextLines))
        {
            sb.AppendLine(line.Text);
        }
        return sb.GetStringAndRelease();
    }
}
