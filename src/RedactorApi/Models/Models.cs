using RedactorApi.Analyzer.Models;
using RedactorApi.FileScanners;

namespace RedactorApi.Models;

public partial record PostReviewResult(string Review, string Text, Analysis[] Replacements);

public partial record FileReviewResult(List<FileReview> Reviews, int TotalFiles);

public partial record UnSuccessfulVerificationResult(bool Verified, string Review);

public partial record VerificationResult(bool Verified, string Review, IEnumerable<Analysis> Replacements, string Content);