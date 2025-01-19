using System.ComponentModel.DataAnnotations;
using RedactorApi.Analyzer.Models;

namespace RedactorApi.FileScanners;

public class FileUploadRequest
{
    [Required]
    public required IFormFileCollection File
    {
        get; set;
    }
    public float? Threshold
    {
        get; set;
    }
    public string? StartTag
    {
        get; set;
    }
    public string? EndTag
    {
        get; set;
    }
    public string? Language
    {
        get; set;
    }
    public ReplacementTextType? ReplacementType
    {
        get; set;
    }
}