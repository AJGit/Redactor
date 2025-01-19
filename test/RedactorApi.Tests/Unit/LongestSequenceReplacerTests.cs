using Moq;
using RedactorApi.Analyzer.Models;
using RedactorApi.Analyzer.Replacer;

namespace RedactorApi.Tests.Unit;

public class LongestSequenceReplacerTests
{
    private readonly LongestSequenceReplacer _replacer;
    private readonly Mock<ILogger<LongestSequenceReplacer>> _loggerMock;

    public LongestSequenceReplacerTests()
    {
        _loggerMock = new Mock<ILogger<LongestSequenceReplacer>>();
        _replacer = new LongestSequenceReplacer(_loggerMock.Object);
    }

    [Fact]
    public void FilterReplacements_ShouldReturnFilteredReplacements_WhenGivenValidInput()
    {
        // Arrange
        var replacements = new List<Analysis>
        {
            new("PERSON", 0.9f, 0, 5),
            new("PERSON",0.8f, 3, 8),
            new("PERSON",0.85f, 6, 10),
        };
        const float threshold = 0.8f;

        // Act
        var result = _replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].Start);
        Assert.Equal(5, result[0].End);
        Assert.Equal(6, result[1].Start);
        Assert.Equal(10, result[1].End);
    }

    [Fact]
    public void FilterReplacements_ShouldReturnEmptyList_WhenNoReplacementsMeetThreshold()
    {
        // Arrange
        var replacements = new List<Analysis>
        {
            new("PERSON", 0.7f, 0, 5),
            new("PERSON",0.6f, 3, 8)

        };
        const float threshold = 0.8f;

        // Act
        var result = _replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterReplacements_ShouldReturnNonOverlappingReplacements_WhenGivenOverlappingReplacements()
    {
        // Arrange
        var replacements = new List<Analysis>
        {
            new("PERSON", 0.9f, 0, 5),
            new("PERSON",0.85f, 4, 9),
            new("PERSON",0.8f, 10, 15),
        };
        const float threshold = 0.8f;

        // Act
        var result = _replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].Start);
        Assert.Equal(5, result[0].End);
        Assert.Equal(10, result[1].Start);
        Assert.Equal(15, result[1].End);
    }
}