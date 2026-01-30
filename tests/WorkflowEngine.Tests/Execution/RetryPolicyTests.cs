using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution;

namespace WorkflowEngine.Tests.Execution;

public class RetryPolicyTests
{
    private readonly DefaultRetryPolicy _policy = new(NullLogger<DefaultRetryPolicy>.Instance);

    [Fact]
    public async Task ExecuteAsync_SuccessOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        var settings = new RetrySettings { MaxRetries = 3, DelayMs = 10 };
        var attempts = 0;

        // Act
        var result = await _policy.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.FromResult("success");
            },
            settings);

        // Assert
        result.Should().Be("success");
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_FailsThenSucceeds_RetriesUntilSuccess()
    {
        // Arrange
        var settings = new RetrySettings { MaxRetries = 3, DelayMs = 10 };
        var attempts = 0;

        // Act
        var result = await _policy.ExecuteAsync<TaskResult>(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                    return Task.FromResult(CreateFailedResult());
                return Task.FromResult(CreateSuccessResult());
            },
            settings);

        // Assert
        result.IsSuccess.Should().BeTrue();
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMaxRetries_ReturnsLastResult()
    {
        // Arrange
        var settings = new RetrySettings { MaxRetries = 2, DelayMs = 10 };
        var attempts = 0;

        // Act
        var result = await _policy.ExecuteAsync<TaskResult>(
            _ =>
            {
                attempts++;
                return Task.FromResult(CreateFailedResult());
            },
            settings);

        // Assert
        result.IsFailed.Should().BeTrue();
        attempts.Should().Be(3); // Initial + 2 retries
    }

    [Fact]
    public async Task ExecuteAsync_WithException_RetriesOnException()
    {
        // Arrange
        var settings = new RetrySettings { MaxRetries = 2, DelayMs = 10 };
        var attempts = 0;

        // Act
        var result = await _policy.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 2)
                    throw new InvalidOperationException("Transient error");
                return Task.FromResult("recovered");
            },
            settings);

        // Assert
        result.Should().Be("recovered");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_CallsOnRetryCallback()
    {
        // Arrange
        var settings = new RetrySettings { MaxRetries = 2, DelayMs = 10 };
        var retryCallbackInvoked = false;
        var callbackAttempt = 0;

        // Act
        var result = await _policy.ExecuteAsync(
            _ =>
            {
                if (!retryCallbackInvoked)
                    throw new InvalidOperationException("Fail first time");
                return Task.FromResult("success");
            },
            settings,
            onRetry: (attempt, ex) =>
            {
                retryCallbackInvoked = true;
                callbackAttempt = attempt;
            });

        // Assert
        retryCallbackInvoked.Should().BeTrue();
        callbackAttempt.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ThrowsOperationCancelledException()
    {
        // Arrange
        var settings = new RetrySettings { MaxRetries = 3, DelayMs = 100 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _policy.ExecuteAsync(
                _ => Task.FromResult("never reached"),
                settings,
                cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_NoRetries_ExecutesOnce()
    {
        // Arrange
        var settings = RetrySettings.None;
        var attempts = 0;

        // Act
        var result = await _policy.ExecuteAsync<TaskResult>(
            _ =>
            {
                attempts++;
                return Task.FromResult(CreateFailedResult());
            },
            settings);

        // Assert
        attempts.Should().Be(1);
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithFailedTaskResult_ReturnsTrue()
    {
        // Arrange
        var result = CreateFailedResult();

        // Act
        var shouldRetry = _policy.ShouldRetry(result);

        // Assert
        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithSuccessfulTaskResult_ReturnsFalse()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var shouldRetry = _policy.ShouldRetry(result);

        // Assert
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithSkippedTaskResult_ReturnsFalse()
    {
        // Arrange
        var result = TaskResult.Skipped("test", "condition not met");

        // Act
        var shouldRetry = _policy.ShouldRetry(result);

        // Assert
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithNonTaskResult_ReturnsFalse()
    {
        // Act
        var shouldRetry = _policy.ShouldRetry("some string");

        // Assert
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithExponentialBackoff_IncreasesDelay()
    {
        // Arrange
        var settings = new RetrySettings
        {
            MaxRetries = 3,
            DelayMs = 50,
            UseExponentialBackoff = true,
            MaxDelayMs = 1000
        };
        var timestamps = new List<DateTimeOffset>();

        // Act
        await _policy.ExecuteAsync<TaskResult>(
            _ =>
            {
                timestamps.Add(DateTimeOffset.UtcNow);
                return Task.FromResult(CreateFailedResult());
            },
            settings);

        // Assert - delays should increase (50, 100, 200)
        timestamps.Should().HaveCount(4); // Initial + 3 retries

        var delay1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var delay2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;
        var delay3 = (timestamps[3] - timestamps[2]).TotalMilliseconds;

        // Each delay should be greater than the previous (exponential backoff)
        // Using >= to account for timing variance on busy systems
        delay2.Should().BeGreaterThanOrEqualTo(delay1 * 1.2, "second delay should be larger than first");
        delay3.Should().BeGreaterThanOrEqualTo(delay2 * 1.2, "third delay should be larger than second");
    }

    [Fact]
    public void FromTask_CreatesCorrectSettings()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo test",
            RetryCount = 5,
            RetryDelayMs = 2000
        };

        // Act
        var settings = RetrySettings.FromTask(task);

        // Assert
        settings.MaxRetries.Should().Be(5);
        settings.DelayMs.Should().Be(2000);
    }

    private static TaskResult CreateSuccessResult() => new()
    {
        TaskId = "test",
        Status = ExecutionStatus.Succeeded,
        ExitCode = 0,
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow
    };

    private static TaskResult CreateFailedResult() => new()
    {
        TaskId = "test",
        Status = ExecutionStatus.Failed,
        ExitCode = 1,
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow,
        ErrorMessage = "Task failed"
    };
}
