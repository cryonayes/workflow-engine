using FluentAssertions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Tests.Webhooks;

public class WebhookModelsTests
{
    [Fact]
    public void WebhookConfig_Defaults_AreCorrect()
    {
        // Arrange & Act
        var config = new WebhookConfig
        {
            Provider = "discord",
            Url = "https://example.com"
        };

        // Assert
        config.Events.Should().Contain(WebhookEventType.WorkflowCompleted);
        config.Events.Should().Contain(WebhookEventType.WorkflowFailed);
        config.Headers.Should().BeEmpty();
        config.Options.Should().BeEmpty();
        config.TimeoutMs.Should().Be(10000);
        config.RetryCount.Should().Be(2);
        config.Name.Should().BeNull();
    }

    [Fact]
    public void WebhookNotification_IsSuccess_ReturnsTrueForSuccessEvents()
    {
        // Arrange
        var completedWorkflow = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        var completedTask = new WebhookNotification
        {
            EventType = WebhookEventType.TaskCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        // Assert
        completedWorkflow.IsSuccess.Should().BeTrue();
        completedTask.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void WebhookNotification_IsFailure_ReturnsTrueForFailureEvents()
    {
        // Arrange
        var failedWorkflow = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowFailed,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        var failedTask = new WebhookNotification
        {
            EventType = WebhookEventType.TaskFailed,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        var timedOutTask = new WebhookNotification
        {
            EventType = WebhookEventType.TaskTimedOut,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test"
        };

        // Assert
        failedWorkflow.IsFailure.Should().BeTrue();
        failedTask.IsFailure.Should().BeTrue();
        timedOutTask.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void WebhookNotification_Summary_FormatsCorrectly()
    {
        // Arrange
        var notification = new WebhookNotification
        {
            EventType = WebhookEventType.WorkflowCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Build Pipeline",
            Duration = TimeSpan.FromSeconds(45.5)
        };

        // Act
        var summary = notification.Summary;

        // Assert
        summary.Should().Contain("Build Pipeline");
        summary.Should().Contain("45.5s");
    }

    [Fact]
    public void WebhookResult_Success_CreatesSuccessfulResult()
    {
        // Arrange & Act
        var result = WebhookResult.Success(200, TimeSpan.FromMilliseconds(150), 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Duration.TotalMilliseconds.Should().Be(150);
        result.Attempts.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void WebhookResult_Failed_CreatesFailedResult()
    {
        // Arrange
        var exception = new HttpRequestException("Connection refused");

        // Act
        var result = WebhookResult.Failed(
            "Connection refused",
            TimeSpan.FromMilliseconds(500),
            statusCode: 503,
            exception: exception,
            attempts: 3);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(503);
        result.Duration.TotalMilliseconds.Should().Be(500);
        result.Attempts.Should().Be(3);
        result.ErrorMessage.Should().Be("Connection refused");
        result.Exception.Should().Be(exception);
    }

    [Theory]
    [InlineData(WebhookEventType.WorkflowStarted)]
    [InlineData(WebhookEventType.WorkflowCompleted)]
    [InlineData(WebhookEventType.WorkflowFailed)]
    [InlineData(WebhookEventType.WorkflowCancelled)]
    [InlineData(WebhookEventType.TaskStarted)]
    [InlineData(WebhookEventType.TaskCompleted)]
    [InlineData(WebhookEventType.TaskFailed)]
    [InlineData(WebhookEventType.TaskSkipped)]
    [InlineData(WebhookEventType.TaskTimedOut)]
    public void WebhookNotification_Summary_HandlesAllEventTypes(WebhookEventType eventType)
    {
        // Arrange
        var notification = new WebhookNotification
        {
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowId = "wf-1",
            RunId = "run-1",
            WorkflowName = "Test Workflow",
            TaskId = "task-1"
        };

        // Act
        var summary = notification.Summary;

        // Assert
        summary.Should().NotBeNullOrEmpty();
    }
}
