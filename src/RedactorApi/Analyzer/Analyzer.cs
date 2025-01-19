using RedactorApi.Analyzer.Models;
using RedactorApi.Analyzer.Replacer;
using RedactorApi.Client;

namespace RedactorApi.Analyzer;

public partial class Analyzer(IPresidioClient presidioClient, IConfiguration configuration, IReplacer replacer, ILogger<Analyzer> logger) : IAnalyzer
{
    private const string ConfigurationSecret = "Analyzer:Secret";
    private readonly string _secret = configuration[ConfigurationSecret]!;
    private readonly ILogger<Analyzer> _logger = logger;
    private static readonly Faker.Faker Faker = new();

    public async Task<AnalysisResponse> AnalyzeTextAsync(ReplacementConfig replacementConfig, int page, CancellationToken cancellationToken = default)
    {
        var request = new PresidioAnalysisRequest(replacementConfig.Content, replacementConfig.Language);
        var response = await Analysis(request, cancellationToken);

        var analyzedConfig = new ReplacementConfigWithResponse(replacementConfig.Content, response)
        {
            Threshold = replacementConfig.Threshold,
            StartTag = replacementConfig.StartTag,
            EndTag = replacementConfig.EndTag,
            ReplacementType = replacementConfig.ReplacementType,
            Language = replacementConfig.Language,
        };
        var (replacements, text) = ReplaceOverlappingFragments(analyzedConfig, replacer, _secret);
        var analysisResponse = new AnalysisResponse(replacements, text, replacementConfig.Content, page);
        return analysisResponse;
    }

    public Task<AnalysisResponse> AnalyzeTextAsync(ReplacementConfig config, CancellationToken cancellationToken = default) =>
        AnalyzeTextAsync(config, 1, cancellationToken);

    private Task<PresidioAnalysisResponse> Analysis(PresidioAnalysisRequest request, CancellationToken cancellationToken)
        => presidioClient.AnalyzeAsync(request, cancellationToken);

    private static (Analysis[] Replacements, string Text) ReplaceOverlappingFragments(ReplacementConfigWithResponse replacementConfig, IReplacer replacer, string secret)
    {
        IEnumerable<Analysis> analysisResults = replacementConfig.PresidioAnalysisResponse.Analysis;
        var replacements = replacer.FilterReplacements(replacementConfig.Threshold, analysisResults);
        var replacementFunction = GetReplacementFunction(replacementConfig, secret);

        var result = new StringBuilder();
        var currentIndex = 0;
        foreach (var replacement in replacements)
        {
            // Add text before the replacement
            if (currentIndex < replacement.Start)
            {
                result.Append(replacementConfig.Content[currentIndex..replacement.Start]);
            }
            Console.WriteLine($""" "{replacementConfig.Content[replacement.Start..replacement.End]}" {replacement.EntityType} [{replacement.Start}..{replacement.End}]""");
            var replacementText = replacementFunction(replacement, replacementConfig.Content);
            result.Append($"{replacementConfig.StartTag}{replacementText}{replacementConfig.EndTag}");
            currentIndex = replacement.End;
        }

        // Append any remaining text after the last replacement
        if (currentIndex < replacementConfig.Content.Length)
        {
            result.Append(replacementConfig.Content[currentIndex..]);
        }

        return (replacements.ToArray(), result.ToString());
    }

    private static Func<Analysis, string, string> GetReplacementFunction(ReplacementConfigWithResponse replacementConfig, string secret)
    {
        return replacementConfig.ReplacementType switch
        {
            ReplacementTextType.Obfuscated => (analysis, content) => ObfuscateAnalysis(analysis,content,secret),
            ReplacementTextType.Fake => FakeAnalysis,
            ReplacementTextType.Original => OriginalAnalysis,
            ReplacementTextType.EntityType => EntityTypeAnalysis,
            _ => throw new InvalidOperationException("Invalid replacement type")
        };
    }

    private static string FakeAnalysis(Analysis analysis, string content) => Faker.GetFakeData(analysis.EntityType);
    private static string ObfuscateAnalysis(Analysis analysis, string content, string secret) => CryptoHelper.EncryptString(content[analysis.Start..analysis.End], secret);
    private static string OriginalAnalysis(Analysis analysis, string content) => content[analysis.Start..analysis.End];
    private static string EntityTypeAnalysis(Analysis analysis, string content) => analysis.EntityType;
}