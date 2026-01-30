using FluentAssertions;
using WorkflowEngine.Console.Rendering;
using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Tests.Console.Rendering;

public class TaskStyleTests
{
    [Fact]
    public void ForWorkflow_Running_ReturnsYellowStyle()
    {
        // Act
        var style = TaskStyle.ForWorkflow(running: true, failed: false);

        // Assert
        style.Icon.Should().Be("●");
        style.Color.Should().Be("yellow");
    }

    [Fact]
    public void ForWorkflow_Paused_ReturnsCyanStyle()
    {
        // Act
        var style = TaskStyle.ForWorkflow(running: true, failed: false, paused: true);

        // Assert
        style.Icon.Should().Be("◉");
        style.Color.Should().Be("cyan");
    }

    [Fact]
    public void ForWorkflow_CompletedWithFailure_ReturnsRedStyle()
    {
        // Act
        var style = TaskStyle.ForWorkflow(running: false, failed: true);

        // Assert
        style.Icon.Should().Be("✗");
        style.Color.Should().Be("red");
    }

    [Fact]
    public void ForWorkflow_CompletedSuccess_ReturnsGreenStyle()
    {
        // Act
        var style = TaskStyle.ForWorkflow(running: false, failed: false);

        // Assert
        style.Icon.Should().Be("✓");
        style.Color.Should().Be("green");
    }

    [Fact]
    public void ForWave_Pending_ReturnsGreyStyle()
    {
        // Act
        var style = TaskStyle.ForWave(WaveStatus.Pending);

        // Assert
        style.Color.Should().Be("grey");
        style.Text.Should().Be("pending");
    }

    [Fact]
    public void ForWave_Running_ReturnsYellowStyle()
    {
        // Act
        var style = TaskStyle.ForWave(WaveStatus.Running);

        // Assert
        style.Color.Should().Be("yellow");
        style.Text.Should().Be("running");
    }

    [Fact]
    public void ForWave_Completed_ReturnsGreenStyle()
    {
        // Act
        var style = TaskStyle.ForWave(WaveStatus.Completed);

        // Assert
        style.Color.Should().Be("green");
        style.Text.Should().Be("done");
    }

    [Theory]
    [InlineData(ExecutionStatus.Pending, "○", "grey")]
    [InlineData(ExecutionStatus.Running, "●", "yellow")]
    [InlineData(ExecutionStatus.Succeeded, "✓", "green")]
    [InlineData(ExecutionStatus.Failed, "✗", "red")]
    [InlineData(ExecutionStatus.Skipped, "⊖", "blue")]
    [InlineData(ExecutionStatus.Cancelled, "⊘", "orange1")]
    public void ForTask_ReturnsCorrectStyleForStatus(ExecutionStatus status, string expectedIcon, string expectedColor)
    {
        // Act
        var style = TaskStyle.ForTask(status);

        // Assert
        style.Icon.Should().Be(expectedIcon);
        style.Color.Should().Be(expectedColor);
    }
}
