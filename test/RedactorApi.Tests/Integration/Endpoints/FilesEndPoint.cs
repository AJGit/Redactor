using System.Globalization;
using System.Net;
using System.Text.Json;
using RedactorApi.Models;
using Xunit.Abstractions;

namespace RedactorApi.Tests.Integration.Endpoints;
public class FileEndpointTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger _logger;
    private readonly HttpClient _client;

    private const string PdfFile = "sample.pdf";
    private const string WordFile = "sample.docx";
    private const string CsvFile = "sample.csv";
    private const string Excel = "sample.xlsx";
    private const float Threshold = 0.6f;

    public FileEndpointTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _logger = XUnitLogger.CreateLogger<FileEndpointTests>(_testOutputHelper);
        _client = new WebApiApplication(_testOutputHelper).CreateClient();
    }

    [Theory]
    [InlineData(PdfFile)]
    [InlineData(WordFile)]
    [InlineData(CsvFile)]
    [InlineData(Excel)]
    // In order to get the integration test to pass, you need to have the files in the Files folder.
    // and an instance of the Presidio API running on localhost:7002
    public async Task PostFile_ReturnsValidIssuesAboveThreshold(string fileName)
    {
        // Arrange
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(File.OpenRead($"./Files/{fileName}")), "file", $"./Files/{fileName}");
        content.Add(new StringContent(Threshold.ToString(CultureInfo.InvariantCulture)), "threshold");
        content.Add(new StringContent("en"), "language");

        // Act
        var response = await _client.PostAsync(Routes.FileCheck.Files, content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stringResult = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(stringResult);
        var result = JsonSerializer.Deserialize<FileReviewResult>(stringResult, JsonOptionConstants.SerializerOptions);

        // Assert
        Assert.NotNull(result);
    }
}
