using FluentAssertions;
using WorkflowEngine.Triggers.Models;
using WorkflowEngine.Triggers.Services;

namespace WorkflowEngine.Tests.Triggers;

public class TriggerMatchResultTests
{
    private readonly TemplateResolver _resolver = new();

    [Fact]
    public void Success_CreatesMatchResult()
    {
        // Arrange
        var rule = CreateRule();
        var message = CreateMessage();
        var captures = new Dictionary<string, string> { ["project"] = "my-api" };

        // Act
        var result = TriggerMatchResult.Success(rule, message, captures);

        // Assert
        result.IsMatch.Should().BeTrue();
        result.Rule.Should().Be(rule);
        result.Message.Should().Be(message);
        result.Captures.Should().ContainKey("project");
    }

    [Fact]
    public void NoMatch_CreatesFailedResult()
    {
        // Act
        var result = TriggerMatchResult.NoMatch();

        // Assert
        result.IsMatch.Should().BeFalse();
        result.Rule.Should().BeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void TemplateResolver_ResolvesCaptures()
    {
        // Arrange
        var message = CreateMessage();
        var captures = new Dictionary<string, string> { ["project"] = "my-api" };

        // Act
        var resolved = _resolver.Resolve("Building {project}...", captures, message);

        // Assert
        resolved.Should().Be("Building my-api...");
    }

    [Fact]
    public void TemplateResolver_ResolvesMessageContext()
    {
        // Arrange
        var message = CreateMessage();
        var captures = new Dictionary<string, string>();

        // Act
        var resolved = _resolver.Resolve("Triggered by {username} via {source}", captures, message);

        // Assert
        resolved.Should().Be("Triggered by john via Telegram");
    }

    [Fact]
    public void TemplateResolver_ResolvesAdditionalValues()
    {
        // Arrange
        var message = CreateMessage();
        var captures = new Dictionary<string, string>();
        var additional = new Dictionary<string, string> { ["runId"] = "run-abc123" };

        // Act
        var resolved = _resolver.Resolve("Run ID: {runId}", captures, message, additional);

        // Assert
        resolved.Should().Be("Run ID: run-abc123");
    }

    [Fact]
    public void TemplateResolver_ResolvesParameters()
    {
        // Arrange
        var message = CreateMessage();
        var captures = new Dictionary<string, string> { ["project"] = "my-api" };
        var parameters = new Dictionary<string, string>
        {
            ["PROJECT_NAME"] = "{project}",
            ["TRIGGERED_BY"] = "{username}"
        };

        // Act
        var resolved = _resolver.ResolveParameters(parameters, captures, message);

        // Assert
        resolved["PROJECT_NAME"].Should().Be("my-api");
        resolved["TRIGGERED_BY"].Should().Be("john");
    }

    [Fact]
    public void TemplateResolver_HandlesNullUsername()
    {
        // Arrange
        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Http,
            Text = "/test",
            Username = null
        };
        var captures = new Dictionary<string, string>();

        // Act
        var resolved = _resolver.Resolve("User: {username}", captures, message);

        // Assert
        resolved.Should().Be("User: unknown");
    }

    private static TriggerRule CreateRule() => new()
    {
        Name = "test",
        Sources = [TriggerSource.Telegram],
        Type = TriggerType.Command,
        Pattern = "/build {project}",
        WorkflowPath = "/test.yaml"
    };

    private static IncomingMessage CreateMessage() => new()
    {
        MessageId = "123",
        Source = TriggerSource.Telegram,
        Text = "/build my-api",
        Username = "john"
    };
}
