using FluentAssertions;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Tests.Core.Utilities;

public class DurationParserTests
{
    [Theory]
    [InlineData("500ms", 500)]
    [InlineData("1000ms", 1000)]
    [InlineData("0ms", 0)]
    public void Parse_WithMilliseconds_ReturnsCorrectTimeSpan(string input, int expectedMs)
    {
        var result = DurationParser.Parse(input);
        result.TotalMilliseconds.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData("1s", 1000)]
    [InlineData("2s", 2000)]
    [InlineData("1.5s", 1500)]
    [InlineData("0.5s", 500)]
    public void Parse_WithSeconds_ReturnsCorrectTimeSpan(string input, int expectedMs)
    {
        var result = DurationParser.Parse(input);
        result.TotalMilliseconds.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData("1m", 60000)]
    [InlineData("2m", 120000)]
    [InlineData("0.5m", 30000)]
    public void Parse_WithMinutes_ReturnsCorrectTimeSpan(string input, int expectedMs)
    {
        var result = DurationParser.Parse(input);
        result.TotalMilliseconds.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData("500", 500)]
    [InlineData("1000", 1000)]
    public void Parse_WithRawNumber_TreatsAsMilliseconds(string input, int expectedMs)
    {
        var result = DurationParser.Parse(input);
        result.TotalMilliseconds.Should().Be(expectedMs);
    }

    [Fact]
    public void Parse_WithNull_ReturnsDefault()
    {
        var result = DurationParser.Parse(null);
        result.TotalMilliseconds.Should().Be(500);
    }

    [Fact]
    public void Parse_WithEmpty_ReturnsDefault()
    {
        var result = DurationParser.Parse("");
        result.TotalMilliseconds.Should().Be(500);
    }

    [Fact]
    public void Parse_WithWhitespace_ReturnsDefault()
    {
        var result = DurationParser.Parse("   ");
        result.TotalMilliseconds.Should().Be(500);
    }

    [Fact]
    public void Parse_WithInvalidFormat_ReturnsDefault()
    {
        var result = DurationParser.Parse("invalid");
        result.TotalMilliseconds.Should().Be(500);
    }

    [Theory]
    [InlineData("  500ms  ")]
    [InlineData("500MS")]
    [InlineData("500Ms")]
    public void Parse_WithWhitespaceAndCase_HandlesCorrectly(string input)
    {
        var result = DurationParser.Parse(input);
        result.TotalMilliseconds.Should().Be(500);
    }

    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        var success = DurationParser.TryParse("1s", out var result);

        success.Should().BeTrue();
        result.TotalSeconds.Should().Be(1);
    }

    [Fact]
    public void TryParse_WithInvalidInput_ReturnsFalse()
    {
        var success = DurationParser.TryParse("invalid", out var result);

        success.Should().BeFalse();
        result.TotalMilliseconds.Should().Be(500); // Default
    }
}
