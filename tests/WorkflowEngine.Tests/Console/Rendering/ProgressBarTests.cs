using FluentAssertions;
using WorkflowEngine.Console.Rendering;

namespace WorkflowEngine.Tests.Console.Rendering;

public class ProgressBarTests
{
    [Fact]
    public void Build_ZeroPercent_ReturnsAllEmpty()
    {
        // Act
        var result = ProgressBar.Build(0, 10, '█', '░', "green", "grey");

        // Assert
        result.Should().Be("[green][/][grey]░░░░░░░░░░[/]");
    }

    [Fact]
    public void Build_FiftyPercent_ReturnsHalfFilled()
    {
        // Act
        var result = ProgressBar.Build(50, 10, '█', '░', "green", "grey");

        // Assert
        result.Should().Be("[green]█████[/][grey]░░░░░[/]");
    }

    [Fact]
    public void Build_HundredPercent_ReturnsAllFilled()
    {
        // Act
        var result = ProgressBar.Build(100, 10, '█', '░', "green", "grey");

        // Assert
        result.Should().Be("[green]██████████[/][grey][/]");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 1)]
    [InlineData(25, 2)]
    [InlineData(33, 3)]
    [InlineData(75, 7)]
    [InlineData(99, 9)]
    [InlineData(100, 10)]
    public void Build_VariousPercentages_CalculatesCorrectFillCount(int percent, int expectedFilledCount)
    {
        // Act
        var result = ProgressBar.Build(percent, 10, '█', '░', "green", "grey");

        // Assert
        var filledCount = result.Split("[/]")[0].Count(c => c == '█');
        filledCount.Should().Be(expectedFilledCount);
    }

    [Fact]
    public void Build_CustomCharacters_UsesProvidedCharacters()
    {
        // Act
        var result = ProgressBar.Build(50, 4, '━', '─', "yellow", "dim");

        // Assert
        result.Should().Be("[yellow]━━[/][dim]──[/]");
    }
}
