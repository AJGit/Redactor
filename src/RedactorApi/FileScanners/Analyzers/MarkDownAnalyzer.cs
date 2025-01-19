using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class MarkDownAnalyzer(IAnalyzer analyzer, ILogger<MarkDownAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<MarkDownAnalyzer> _logger = logger;

    internal override string SupportedExtension => ".md";

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream markdownStream)
    {
        using var _ = LogUtils.Create(_logger, nameof(MarkDownAnalyzer), nameof(ExtractTextAsync));
        using var reader = new StreamReader(markdownStream, Encoding.UTF8);
        var markdown = (await reader.ReadToEndAsync()).Trim(); 

        if(string.IsNullOrWhiteSpace(markdown))
        {
            yield break;
        }

        // Parse the markdown document using Markdig
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(markdown, pipeline);

        var sb = StringBuilderCache.Acquire();

        // Iterate over all top-level blocks in the document
        foreach (var block in document)
        {
            // Many textual blocks such as ParagraphBlock contain inline elements
            if (block is LeafBlock { Inline: not null } leafBlock)
            {
                foreach (var inline in leafBlock.Inline)
                {
                    // We're interested in literal text
                    if (inline is LiteralInline literalInline)
                    {
                        sb.Append(literalInline.Content.ToString());
                        sb.Append(' ');
                    }
                }
            }

            // Optionally add a line break after each block (depending on desired formatting)
            sb.AppendLine();
        }

        yield return new PageInfo(1, sb.TrimAndRelease());
    }
}