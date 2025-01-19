using System.Net;
using System.Text;
using System.Text.Json;
using RedactorApi.Analyzer.Models;
using RedactorApi.Models;
using Xunit.Abstractions;

namespace RedactorApi.Tests.Integration.Endpoints;
public class VerifyEndpointTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger _logger;
    private readonly HttpClient _client;

    public VerifyEndpointTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _logger = XUnitLogger.CreateLogger<VerifyEndpointTests>(_testOutputHelper);
        _client = new WebApiApplication(_testOutputHelper).CreateClient();
    }

    [Fact]
    // In order to get the integration test to pass, you need an instance of the Presidio API running on localhost:7002
    public async Task PostVerify_ReturnsValidReplacementsAboveThreshold()
    {
        const float threshold = 0.6f;
        // Arrange
        var requestFields = new ReplacementConfig(

            Content: """
                      Purchase Order
                      ----------------
                      Date: 10/05/2023
                      ----------------
                      Customer Name: CID-982305
                      Billing Address: 1234 Oak Street, 
                                       Suite 400, 
                                       Springfield, 
                                       IL, 
                                       EN12 4TA
                      Phone: (312) 555-7890 (555-876-5432)
                      Email: janedoe@company.com
                      """,
            Threshold: threshold,
            Language: "en",
            StartTag: "<span style:'color:red'>",
            EndTag: "</span>",
            ReplacementType: ReplacementTextType.EntityType
        );
        var json = JsonSerializer.Serialize(requestFields);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync(Routes.Verification.Verify, content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stringResult = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(stringResult);
        var result = JsonSerializer.Deserialize<VerificationResult>(stringResult, JsonOptionConstants.SerializerOptions);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Replacements.Any());
        Assert.All(result.Replacements, replacement => Assert.True(replacement.Score > threshold));
        Assert.True(result.Verified);
    }
}

