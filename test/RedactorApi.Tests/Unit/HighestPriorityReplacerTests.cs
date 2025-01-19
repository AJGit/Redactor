using Moq;
using RedactorApi.Analyzer.Models;
using RedactorApi.Analyzer.Replacer;

namespace RedactorApi.Tests.Unit;

public class HighestPriorityReplacerTests
{
    private readonly HighestPriorityReplacer _replacer;
    private readonly Mock<ILogger<HighestPriorityReplacer>> _loggerMock;

    public HighestPriorityReplacerTests()
    {
        _loggerMock = new Mock<ILogger<HighestPriorityReplacer>>();
        _replacer = new HighestPriorityReplacer(_loggerMock.Object);
    }

    [Fact]
    public void FilterReplacements_ShouldReturnFilteredReplacements_WhenGivenValidInput()
    {
        // Arrange
        var replacements = new List<Analysis>
        {
            new("PERSON", 0.95f, 0, 5),
            new("PERSON",0.90f, 6, 10),
            new("PERSON",0.85f, 4, 8)
        };
        const float threshold = 0.90f;

        // Act
        var result = _replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r is { Start: 0, End: 5 });
        Assert.Contains(result, r => r is { Start: 6, End: 10 });
    }

    [Fact]
    public void FilterReplacements_ShouldReturnEmptyList_WhenNoReplacementsMeetThreshold()
    {
        // Arrange
        var replacements = new List<Analysis>
        {
            new("PERSON",0.80f, 0, 5),
            new("PERSON",0.85f, 6, 10)
        };
        const float threshold = 0.90f;

        // Act
        var result = _replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterReplacements_ShouldReturnNonOverlappingReplacements()
    {
        // Arrange
        var replacements = new List<Analysis>
        {
            new ("PERSON", 0.95f, 0, 5),
            new ("PERSON",0.90f, 9, 12),
            new ("PERSON",0.85f, 4, 8)
        };
        const float threshold = 0.80f;

        // Act
        var result = _replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r is { Start: 0, End: 5 });
        Assert.Contains(result, r => r is { Start: 9, End: 12 });
    }
}