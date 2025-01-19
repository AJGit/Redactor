using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class PowerPointAnalyzer(IAnalyzer analyzer, ILogger<ScannerBase> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<ScannerBase> _logger = logger;
    internal override string SupportedExtension => ".pptx";
    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream pptxStream)
    {
        using var _ = LogUtils.Create(_logger, nameof(PowerPointAnalyzer), nameof(ExtractTextAsync));
        var slideCounter = 0;
        using var presentationDocument = PresentationDocument.Open(pptxStream, false);
        var presentationPart = presentationDocument.PresentationPart;
        if (presentationPart?.Presentation?.SlideIdList == null)
        {
            yield break;
        }

        // For each slide in the presentation
        foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
        {
            var sb = StringBuilderCache.Acquire();
            var relationshipId = slideId.RelationshipId;
            if (relationshipId == null)
            {
                continue;
            }

            if (presentationPart.GetPartById(relationshipId!) is not SlidePart slidePart)
            {
                continue;
            }

            slideCounter++;
            sb.Clear();
            // Slides contain shapes, and shapes may contain text
            foreach (var shape in slidePart.Slide.Descendants<Shape>())
            {
                var textBody = shape.TextBody;
                if (textBody == null)
                {
                    continue;
                }

                // Extract text from each paragraph and run
                foreach (var paragraph in textBody.Descendants<Paragraph>())
                {
                    foreach (var run in paragraph.Descendants<Run>())
                    {
                        sb.Append(run.InnerText);
                    }
                    sb.AppendLine(); // New line after each paragraph
                }
            }
            if (sb.Length > 0)
            {
                yield return await ValueTask.FromResult(new PageInfo(slideCounter, sb.TrimAndRelease()));
            }
        }
    }
}
