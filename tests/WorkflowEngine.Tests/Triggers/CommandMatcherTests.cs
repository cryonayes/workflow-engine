using FluentAssertions;
using WorkflowEngine.Triggers.Matching;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Tests.Triggers;

public class CommandMatcherTests
{
    private readonly CommandMatcher _matcher = new();

    [Fact]
    public void TryMatch_SimpleCommand_MatchesExactly()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/help");
        var message = CreateMessage("/help");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void TryMatch_CommandWithParameter_CapturesValue()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/build {project}");
        var message = CreateMessage("/build my-api");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["project"].Should().Be("my-api");
    }

    [Fact]
    public void TryMatch_CommandWithMultipleParameters_CapturesAll()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/deploy {service} {version}");
        var message = CreateMessage("/deploy auth-api v1.2.3");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["service"].Should().Be("auth-api");
        result["version"].Should().Be("v1.2.3");
    }

    [Fact]
    public void TryMatch_CaseInsensitive_Matches()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/BUILD {project}");
        var message = CreateMessage("/build MyProject");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["project"].Should().Be("MyProject");
    }

    [Fact]
    public void TryMatch_NoMatch_ReturnsNull()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/build {project}");
        var message = CreateMessage("/deploy my-api");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_ExtraWhitespace_Matches()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/build {project}");
        var message = CreateMessage("  /build   my-api  ");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["project"].Should().Be("my-api");
    }

    [Fact]
    public void TryMatch_MissingParameter_DoesNotMatch()
    {
        // Arrange
        var rule = CreateRule(TriggerType.Command, "/build {project} {env}");
        var message = CreateMessage("/build my-api");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_NullPattern_ReturnsNull()
    {
        // Arrange
        var rule = new TriggerRule
        {
            Name = "test",
            Sources = [TriggerSource.Telegram],
            Type = TriggerType.Command,
            Pattern = null,
            WorkflowPath = "/test.yaml"
        };
        var message = CreateMessage("/anything");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    private static TriggerRule CreateRule(TriggerType type, string pattern) => new()
    {
        Name = "test-rule",
        Sources = [TriggerSource.Telegram],
        Type = type,
        Pattern = pattern,
        WorkflowPath = "/test.yaml"
    };

    private static IncomingMessage CreateMessage(string text) => new()
    {
        MessageId = "test-123",
        Source = TriggerSource.Telegram,
        Text = text,
        Username = "testuser"
    };
}
