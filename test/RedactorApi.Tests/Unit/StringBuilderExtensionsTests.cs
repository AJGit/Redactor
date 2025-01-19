using System.Text;
using RedactorApi.Util;

namespace RedactorApi.Tests.Unit;

public class StringBuilderExtensionsTests
{
    [Fact]
    public void Trim_ShouldTrimBothEnds()
    {
        // Arrange
        var sb = new StringBuilder("  Hello World  ");

        // Act
        var result = sb.Trim();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Trim_ShouldReturnEmptyString_WhenOnlyWhitespace()
    {
        // Arrange
        var sb = new StringBuilder("   ");

        // Act
        var result = sb.Trim();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TrimStart_ShouldTrimLeadingWhitespace()
    {
        // Arrange
        var sb = new StringBuilder("  Hello World");

        // Act
        var result = sb.TrimStart();

        // Assert
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void TrimStart_ShouldNotChange_WhenNoLeadingWhitespace()
    {
        // Arrange
        var sb = new StringBuilder("Hello World");

        // Act
        var result = sb.TrimStart();

        // Assert
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void TrimEnd_ShouldTrimTrailingWhitespace()
    {
        // Arrange
        var sb = new StringBuilder("Hello World  ");

        // Act
        var result = sb.TrimEnd();

        // Assert
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void TrimEnd_ShouldNotChange_WhenNoTrailingWhitespace()
    {
        // Arrange
        var sb = new StringBuilder("Hello World");

        // Act
        var result = sb.TrimEnd();

        // Assert
        Assert.Equal("Hello World", result.ToString());
    }
}