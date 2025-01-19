using System.Net;
using System.Text;
using System.Text.Json;
using RedactorApi.Analyzer.Models;
using RedactorApi.Models;
using Xunit.Abstractions;

namespace RedactorApi.Tests.Integration.Endpoints;
public class ReviewEndpointTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger _logger;
    private readonly HttpClient _client;

    public ReviewEndpointTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _logger = XUnitLogger.CreateLogger<ReviewEndpointTests>(_testOutputHelper);
        _client = new WebApiApplication(_testOutputHelper).CreateClient();
    }

    [Theory]
    [InlineData(ReplacementTextType.EntityType)]
    [InlineData(ReplacementTextType.Fake)]
    [InlineData(ReplacementTextType.Obfuscated)]
    [InlineData(ReplacementTextType.Original)]
    // In order to get the integration test to pass, you need an instance of the Presidio API running on localhost:7002
    public async Task PostReview_ReturnsValidReplacementsAboveThreshold(ReplacementTextType replacementType)
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
            ReplacementType: replacementType
        );
        var json = JsonSerializer.Serialize(requestFields);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync(Routes.Reviews.Review, content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stringResult = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(stringResult);
        var result = JsonSerializer.Deserialize<PostReviewResult>(stringResult, JsonOptionConstants.SerializerOptions);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Replacements.Length > 0);
        Assert.All(result.Replacements, replacement => Assert.True(replacement.Score > threshold));
    }
}
