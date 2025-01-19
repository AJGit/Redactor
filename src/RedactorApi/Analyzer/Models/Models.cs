using RedactorApi.Analyzer.Converters;

namespace RedactorApi.Analyzer.Models;

public record AnalysisResponse(Analysis[] Replacements, string Text, string OriginalText, int PageNumber);
public record PresidioAnalysisRequest(string Text, string Language);
public record PresidioAnalysisResponse(Analysis[] Analysis);
public record Analysis([property: JsonPropertyName("entity_type")] string EntityType, double Score, int Start, int End);

public record ReplacementConfig(
    string Content,
    [property: JsonConverter(typeof(StringToFloatConverter))]
    float Threshold = 0.5f,
    string StartTag = "{{",
    string EndTag = "}}",
    [property: JsonPropertyName("replacementType")]
    // [property: JsonConverter(typeof(ReplacementTextTypeConverter))]
    ReplacementTextType ReplacementType = ReplacementTextType.EntityType,
    string Language = "en");

internal record ReplacementConfigWithResponse(
    string Content,
    PresidioAnalysisResponse PresidioAnalysisResponse,
    float Threshold = 0.5F,
    string StartTag = "{{",
    string EndTag = "}}",
    ReplacementTextType ReplacementType = ReplacementTextType.EntityType,
    string Language = "en")
    : ReplacementConfig(Content, Threshold, StartTag, EndTag, ReplacementType, Language);