using Moq;
using RedactorApi.Analyzer.Models;
using RedactorApi.Analyzer.Replacer;
using RedactorApi.Client;

namespace RedactorApi.Tests.Unit;

public class AnalyzerTests
{
    private readonly Mock<IPresidioClient> _presidioClientMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IReplacer> _replacerMock;
    private readonly Mock<ILogger<Analyzer.Analyzer>> _loggerMock;
    private readonly Analyzer.Analyzer _analyzer;

    public AnalyzerTests()
    {
        _presidioClientMock = new Mock<IPresidioClient>();
        _configurationMock = new Mock<IConfiguration>();
        _replacerMock = new Mock<IReplacer>();
        _loggerMock = new Mock<ILogger<Analyzer.Analyzer>>();

        _configurationMock.Setup(c => c["Analyzer:Secret"]).Returns("test_secret");

        _analyzer = new Analyzer.Analyzer(
            _presidioClientMock.Object,
            _configurationMock.Object,
            _replacerMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task AnalyzeTextAsync_ShouldReturnAnalysisResponse()
    {
        // Arrange
        var replacementConfig = new ReplacementConfig("Test content", 0.5f, "<start>", "<end>", ReplacementTextType.Fake, "en");

        var presidioResponse = new PresidioAnalysisResponse([new Analysis("PERSON", 0.8, 0, 4)]);

        _presidioClientMock
            .Setup(p => p.AnalyzeAsync(It.IsAny<PresidioAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(presidioResponse);

        _replacerMock
            .Setup(r => r.FilterReplacements(It.IsAny<float>(), It.IsAny<IEnumerable<Analysis>>()))
            .Returns(presidioResponse.Analysis.ToList());

        // Act
        var result = await _analyzer.AnalyzeTextAsync(replacementConfig, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test content", result.OriginalText);
        Assert.Equal(1, result.PageNumber);
        Assert.Single(result.Replacements);
        Assert.Contains("<start>", result.Text);
        Assert.Contains("<end>", result.Text);
    }
    [Fact]
    public async Task Text_ShouldReturnEntityTaggedText()
    {
        // Arrange
        var presidioClient = new Mock<IPresidioClient>();
        var mockLoggerAnalyzer = new Mock<ILogger<Analyzer.Analyzer>>();
        var mockLoggerHighest = new Mock<ILogger<HighestPriorityReplacer>>();
        var mockConfiguration = new Mock<IConfiguration>();

        presidioClient.Setup(x => x.AnalyzeAsync(It.IsAny<PresidioAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresidioAnalysisResponse(
            [
                new Analysis("UK_POST_CODE", 0.7F, 154, 162),
                new Analysis("EMAIL_ADDRESS", 1.0F, 207, 226),
                new Analysis("DATE_TIME", 0.95F, 38, 48),
                new Analysis("LOCATION", 0.85F, 137, 148),
                new Analysis("LOCATION", 0.85F, 150, 152),
                new Analysis("PHONE_NUMBER", 0.75F, 170, 184),
                new Analysis("PHONE_NUMBER", 0.75F, 185, 198),
                new Analysis("URL", 0.5F, 215, 226),
                new Analysis("IN_PAN", 0.05F, 81, 91),
                new Analysis("US_DRIVER_LICENSE", 0.01F, 85, 91)
            ]));

        var replacer = new HighestPriorityReplacer(mockLoggerHighest.Object);
        var analyzer = new Analyzer.Analyzer(presidioClient.Object, mockConfiguration.Object, replacer, mockLoggerAnalyzer.Object);
        var input = "Purchase Order\n----------------\nDate: 10/05/2023\n----------------\nCustomer Name: CID-982305\nBilling Address: 1234 Oak Street, Suite 400, Springfield, IL, EN12 4TA\nPhone: (312) 555-7890 (555-876-5432)\nEmail: janedoe@company.com\n";
        var config = new ReplacementConfig(input);

        // Act
        var result = await analyzer.AnalyzeTextAsync(config);

        // Assert
        Assert.Equal("Purchase Order\n----------------\nDate: {{DATE_TIME}}\n----------------\nCustomer Name: CID-982305\nBilling Address: 1234 Oak Street, Suite 400, {{LOCATION}}, {{LOCATION}}, {{UK_POST_CODE}}\nPhone: {{PHONE_NUMBER}} {{PHONE_NUMBER}})\nEmail: {{EMAIL_ADDRESS}}\n", result.Text);
    }
}