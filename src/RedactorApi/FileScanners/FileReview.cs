namespace RedactorApi.FileScanners;

public record FileReview(string FileName, string Review, long Size, int Pages, int IssueCount, FileAnalysis Analysis);
public record PageAnalysis(int PageNumber, IEnumerable<Issue> Issues);
public record FileAnalysis(PageAnalysis[] Pages);
public record PageInfo(int PageNumber, string Text);
public record Issue(string Value, string Type, double Score, IEnumerable<Location> Locations);
public record Location(int Start, int End);
public record ScanDocumentResults(FileAnalysis FileAnalysis, int PageCount, int IssueCount);