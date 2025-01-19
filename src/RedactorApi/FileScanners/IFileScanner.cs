using RedactorApi.Analyzer.Models;

namespace RedactorApi.FileScanners;

public interface IFileScanner
{
    public string SupportedExtension
    {
        get;
    }
    Task<ScanDocumentResults> ScanDocumentAsync(IFormFile file, ReplacementConfig replacementConfig, CancellationToken cancellationToken);
}

