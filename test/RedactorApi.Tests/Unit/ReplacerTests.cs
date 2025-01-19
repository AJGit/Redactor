using Moq;
using RedactorApi.Analyzer.Models;
using RedactorApi.Analyzer.Replacer;

namespace RedactorApi.Tests.Unit;

public class ReplacerTests
{
    [Fact]
    public void FilterReplacements_HighestPriorityReplacer_FiltersCorrectly()
    {
        // Arrange
        var mockLoggerHighest = new Mock<ILogger<HighestPriorityReplacer>>();
        var replacer = new HighestPriorityReplacer(mockLoggerHighest.Object);
        var replacements = new List<Analysis>
        {
            new ("PERSON",0.95, 0, 5 ),
            new ("PERSON",0.90, 3, 8 ),
            new ("PERSON",0.85, 6, 10 ),
            new ("PERSON",0.80, 9, 12)
        };
        const float threshold = 0.80f;

        // Act
        var result = replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].Start);
        Assert.Equal(5, result[0].End);
        Assert.Equal(6, result[1].Start);
        Assert.Equal(10, result[1].End);
    }

    [Fact]
    public void FilterReplacements_LongestSequenceReplacer_FiltersCorrectly()
    {
        // Arrange
        var mockLoggerSeq = new Mock<ILogger<LongestSequenceReplacer>>();
        var replacer = new LongestSequenceReplacer(mockLoggerSeq.Object);
        var replacements = new List<Analysis>
        {
            new("PERSON",0.95, 0, 5 ),
            new("PERSON",0.90, 3, 8 ),
            new("PERSON",0.85, 6, 10 ),
            new("PERSON",0.80, 9, 12)
        };
        const float threshold = 0.80f;

        // Act
        var result = replacer.FilterReplacements(threshold, replacements);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].Start);
        Assert.Equal(5, result[0].End);
        Assert.Equal(6, result[1].Start);
        Assert.Equal(10, result[1].End);
    }
}
