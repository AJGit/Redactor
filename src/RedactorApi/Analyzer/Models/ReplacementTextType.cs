namespace RedactorApi.Analyzer.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ReplacementTextType>))]
public enum ReplacementTextType
{
    Original,
    EntityType,
    Fake,
    Obfuscated
}