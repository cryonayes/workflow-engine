using FluentAssertions;
using WorkflowEngine.Console.Rendering;

namespace WorkflowEngine.Tests.Console.Rendering;

public class TextFormatterTests
{
    [Theory]
    [InlineData(0, "0.0s")]
    [InlineData(1.5, "1.5s")]
    [InlineData(30.7, "30.7s")]
    [InlineData(59.9, "59.9s")]
    public void FormatDuration_UnderOneMinute_FormatsAsSeconds(double seconds, string expected)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(seconds);

        // Act
        var result = TextFormatter.FormatDuration(duration);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(60, "1m0s")]
    [InlineData(65, "1m5s")]
    [InlineData(125, "2m5s")]
    [InlineData(3600, "60m0s")]
    public void FormatDuration_OneMinuteOrMore_FormatsAsMinutesAndSeconds(double seconds, string expected)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(seconds);

        // Act
        var result = TextFormatter.FormatDuration(duration);

        // Assert
        result.Should().Be(expected);
    }
}
