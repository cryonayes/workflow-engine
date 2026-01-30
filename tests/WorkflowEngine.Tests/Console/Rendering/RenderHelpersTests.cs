using FluentAssertions;
using WorkflowEngine.Console.Rendering;

namespace WorkflowEngine.Tests.Console.Rendering;

public class RenderHelpersTests
{
    [Fact]
    public void Escape_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = RenderHelpers.Escape(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Escape_EmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = RenderHelpers.Escape("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Escape_PlainText_ReturnsUnchanged()
    {
        // Act
        var result = RenderHelpers.Escape("Hello World");

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void Escape_TextWithBrackets_EscapesBrackets()
    {
        // Act
        var result = RenderHelpers.Escape("[red]test[/]");

        // Assert
        result.Should().Be("[[red]]test[[/]]");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void HorizontalRule_ReturnsCorrectWidth(int width)
    {
        // Act
        var result = RenderHelpers.HorizontalRule(width);

        // Assert
        result.Should().StartWith("[grey]");
        result.Should().EndWith("[/]");
        result.Should().Contain(new string('─', width));
    }

    [Fact]
    public void PadLines_AddsCorrectNumberOfEmptyLines()
    {
        // Arrange
        var lines = new List<string> { "line1", "line2" };

        // Act
        RenderHelpers.PadLines(lines, 3);

        // Assert
        lines.Should().HaveCount(5);
        lines[2].Should().BeEmpty();
        lines[3].Should().BeEmpty();
        lines[4].Should().BeEmpty();
    }

    [Fact]
    public void PadLines_ZeroCount_DoesNotModifyList()
    {
        // Arrange
        var lines = new List<string> { "line1" };

        // Act
        RenderHelpers.PadLines(lines, 0);

        // Assert
        lines.Should().HaveCount(1);
    }

    [Fact]
    public void PadLines_NegativeCount_DoesNotModifyList()
    {
        // Arrange
        var lines = new List<string> { "line1" };

        // Act
        RenderHelpers.PadLines(lines, -5);

        // Assert
        lines.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("short", 10, "short")]
    [InlineData("exactly10", 10, "exactly10")]
    [InlineData("hello world", 10, "hello wor…")]
    [InlineData("a very long string that needs truncation", 20, "a very long string …")]
    public void Truncate_TruncatesLongStrings(string input, int maxLength, string expected)
    {
        // Act
        var result = RenderHelpers.Truncate(input, maxLength);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = RenderHelpers.Truncate("", 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_NullString_ReturnsEmpty()
    {
        // Act
        var result = RenderHelpers.Truncate(null!, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_SingleCharWithMaxOne_ReturnsSingleChar()
    {
        // Act
        var result = RenderHelpers.Truncate("a", 1);

        // Assert
        result.Should().Be("a");
    }

    [Fact]
    public void Truncate_TwoCharsWithMaxOne_ReturnsEllipsis()
    {
        // Act
        var result = RenderHelpers.Truncate("ab", 1);

        // Assert
        result.Should().Be("…");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void BuildScrollIndicator_ReturnsCorrectFormat(int percent)
    {
        // Act
        var result = RenderHelpers.BuildScrollIndicator(percent);

        // Assert
        result.Should().StartWith("│");
        result.Should().EndWith("│");
        result.Should().Contain("█");
        result.Length.Should().Be(11); // │ + 8 bar chars + █ + │
    }
}
