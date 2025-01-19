//using RedactorApi.Analyzer.Converters;
using RedactorApi.Analyzer.Models;

namespace RedactorApi.Models;

[JsonSourceGenerationOptions(WriteIndented =  true, 
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, 
    // Converters = [typeof(ReplacementTextTypeConverter)],
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
[JsonSerializable(typeof(ReplacementConfig))]
[JsonSerializable(typeof(PresidioAnalysisRequest))]
[JsonSerializable(typeof(Analysis[]))]
[JsonSerializable(typeof(PostReviewResult))]
[JsonSerializable(typeof(FileReviewResult))]
[JsonSerializable(typeof(UnSuccessfulVerificationResult))]
[JsonSerializable(typeof(VerificationResult))]
public partial class RedactorJsonSerializerContext: JsonSerializerContext
{
}
