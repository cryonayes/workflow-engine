using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Parsing;

namespace WorkflowEngine.Tests.Webhooks;

public class WebhookParsingTests
{
    private readonly YamlWorkflowParser _parser = new(
        new WorkflowValidator(),
        NullLogger<YamlWorkflowParser>.Instance);

    [Fact]
    public void Parse_WorkflowWithWebhooks_ParsesWebhookConfigs()
    {
        // Arrange
        var yaml = """
            name: Webhook Test
            webhooks:
              - provider: discord
                url: https://discord.com/api/webhooks/123
                events: [workflow_completed, workflow_failed]
            tasks:
              - id: build
                run: echo "Building"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Webhooks.Should().HaveCount(1);
        workflow.Webhooks[0].Provider.Should().Be("discord");
        workflow.Webhooks[0].Url.Should().Be("https://discord.com/api/webhooks/123");
        workflow.Webhooks[0].Events.Should().HaveCount(2);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.WorkflowCompleted);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.WorkflowFailed);
    }

    [Fact]
    public void Parse_WebhookWithAllOptions_ParsesAllFields()
    {
        // Arrange
        var yaml = """
            name: Full Webhook Test
            webhooks:
              - provider: http
                url: https://api.example.com/webhook
                name: My Webhook
                headers:
                  Authorization: Bearer token123
                  X-Custom-Header: custom-value
                options:
                  format: json
                timeoutMs: 5000
                retryCount: 3
                events: [workflow_started, task_failed]
            tasks:
              - id: test
                run: echo "test"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        var webhook = workflow.Webhooks[0];
        webhook.Provider.Should().Be("http");
        webhook.Url.Should().Be("https://api.example.com/webhook");
        webhook.Name.Should().Be("My Webhook");
        webhook.TimeoutMs.Should().Be(5000);
        webhook.RetryCount.Should().Be(3);
        webhook.Headers.Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer token123");
        webhook.Headers.Should().ContainKey("X-Custom-Header").WhoseValue.Should().Be("custom-value");
        webhook.Options.Should().ContainKey("format").WhoseValue.Should().Be("json");
        webhook.Events.Should().Contain(WebhookEventType.WorkflowStarted);
        webhook.Events.Should().Contain(WebhookEventType.TaskFailed);
    }

    [Fact]
    public void Parse_MultipleWebhooks_ParsesAllWebhooks()
    {
        // Arrange
        var yaml = """
            name: Multi Webhook Test
            webhooks:
              - provider: discord
                url: https://discord.com/api/webhooks/123
              - provider: slack
                url: https://hooks.slack.com/services/xxx
              - provider: telegram
                url: https://api.telegram.org/bot123/sendMessage
                options:
                  chat_id: "-1001234567890"
            tasks:
              - id: build
                run: echo "Building"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Webhooks.Should().HaveCount(3);
        workflow.Webhooks[0].Provider.Should().Be("discord");
        workflow.Webhooks[1].Provider.Should().Be("slack");
        workflow.Webhooks[2].Provider.Should().Be("telegram");
        workflow.Webhooks[2].Options.Should().ContainKey("chat_id");
    }

    [Fact]
    public void Parse_WebhookWithDefaultEvents_UsesDefaultEventTypes()
    {
        // Arrange
        var yaml = """
            name: Default Events Test
            webhooks:
              - provider: discord
                url: https://discord.com/api/webhooks/123
            tasks:
              - id: build
                run: echo "Building"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.WorkflowCompleted);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.WorkflowFailed);
    }

    [Fact]
    public void Parse_WorkflowWithoutWebhooks_HasEmptyWebhooksList()
    {
        // Arrange
        var yaml = """
            name: No Webhooks
            tasks:
              - id: build
                run: echo "Building"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Webhooks.Should().BeEmpty();
    }

    [Theory]
    [InlineData("workflow_started", WebhookEventType.WorkflowStarted)]
    [InlineData("workflow_completed", WebhookEventType.WorkflowCompleted)]
    [InlineData("workflow_failed", WebhookEventType.WorkflowFailed)]
    [InlineData("workflow_cancelled", WebhookEventType.WorkflowCancelled)]
    [InlineData("task_started", WebhookEventType.TaskStarted)]
    [InlineData("task_completed", WebhookEventType.TaskCompleted)]
    [InlineData("task_failed", WebhookEventType.TaskFailed)]
    [InlineData("task_skipped", WebhookEventType.TaskSkipped)]
    [InlineData("task_timed_out", WebhookEventType.TaskTimedOut)]
    public void Parse_WebhookEventType_ParsesCorrectly(string eventString, WebhookEventType expected)
    {
        // Arrange
        var yaml = $"""
            name: Event Type Test
            webhooks:
              - provider: http
                url: https://example.com
                events: [{eventString}]
            tasks:
              - id: test
                run: echo "test"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Webhooks[0].Events.Should().Contain(expected);
    }

    [Fact]
    public void Parse_WebhookWithTaskEvents_ParsesAllTaskEventTypes()
    {
        // Arrange
        var yaml = """
            name: Task Events Test
            webhooks:
              - provider: slack
                url: https://hooks.slack.com/services/xxx
                events: [task_started, task_completed, task_failed, task_skipped, task_timed_out]
            tasks:
              - id: test
                run: echo "test"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Webhooks[0].Events.Should().HaveCount(5);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.TaskStarted);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.TaskCompleted);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.TaskFailed);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.TaskSkipped);
        workflow.Webhooks[0].Events.Should().Contain(WebhookEventType.TaskTimedOut);
    }
}
