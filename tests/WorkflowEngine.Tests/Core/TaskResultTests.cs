using FluentAssertions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Tests.Core;

public class TaskResultTests
{
    [Fact]
    public void IsCancelled_ReturnsTrue_WhenStatusIsCancelled()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Cancelled
        };

        // Assert
        result.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void IsCancelled_ReturnsFalse_WhenStatusIsSucceeded()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Succeeded
        };

        // Assert
        result.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsCancelled_ReturnsFalse_WhenStatusIsFailed()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Failed
        };

        // Assert
        result.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsFailed_ReturnsTrue_WhenStatusIsCancelled()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Cancelled
        };

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void IsFailed_ReturnsTrue_WhenStatusIsFailed()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Failed
        };

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void IsFailed_ReturnsTrue_WhenStatusIsTimedOut()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.TimedOut
        };

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void IsFailed_ReturnsFalse_WhenStatusIsSucceeded()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Succeeded
        };

        // Assert
        result.IsFailed.Should().BeFalse();
    }

    [Fact]
    public void Cancelled_FactoryMethod_CreatesCorrectResult()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);

        // Act
        var result = TaskResult.Cancelled("task1", startTime);

        // Assert
        result.TaskId.Should().Be("task1");
        result.Status.Should().Be(ExecutionStatus.Cancelled);
        result.IsCancelled.Should().BeTrue();
        result.IsFailed.Should().BeTrue();
        result.ExitCode.Should().Be(-1);
        result.StartTime.Should().Be(startTime);
        result.ErrorMessage.Should().Be("Task was cancelled");
    }

    [Fact]
    public void IsSuccess_ReturnsFalse_WhenStatusIsCancelled()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Cancelled
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WasSkipped_ReturnsFalse_WhenStatusIsCancelled()
    {
        // Arrange
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Cancelled
        };

        // Assert
        result.WasSkipped.Should().BeFalse();
    }
}
