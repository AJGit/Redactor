namespace RedactorApi.FileScanners;

public class FileAnalyzerFactory(IEnumerable<IFileScanner> analyzers)
{
    private readonly IEnumerable<IFileScanner> _analyzers = analyzers;

    public IFileScanner GetAnalyzer(string fileExtension)
    {
        return (_analyzers.FirstOrDefault(a => a.SupportedExtension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ??
                _analyzers.FirstOrDefault(a => a.SupportedExtension.Equals(string.Empty, StringComparison.OrdinalIgnoreCase)))!;
    }
}
