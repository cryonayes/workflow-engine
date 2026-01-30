using FluentAssertions;
using WorkflowEngine.Triggers.Matching;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Tests.Triggers;

public class PatternMatcherTests
{
    private readonly PatternMatcher _matcher = new();

    [Fact]
    public void TryMatch_SimpleRegex_Matches()
    {
        // Arrange
        var rule = CreateRule("deploy\\s+\\w+");
        var message = CreateMessage("deploy auth-api");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void TryMatch_NamedCaptureGroups_CapturesValues()
    {
        // Arrange
        var rule = CreateRule(@"deploy\s+(?<service>\w[\w-]*)\s+v(?<version>[\d.]+)");
        var message = CreateMessage("deploy auth-api v1.2.3");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["service"].Should().Be("auth-api");
        result["version"].Should().Be("1.2.3");
    }

    [Fact]
    public void TryMatch_NoMatch_ReturnsNull()
    {
        // Arrange
        var rule = CreateRule(@"^deploy\s+");
        var message = CreateMessage("build my-project");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_CaseInsensitive_Matches()
    {
        // Arrange
        var rule = CreateRule(@"DEPLOY\s+(?<service>\w+)");
        var message = CreateMessage("deploy myservice");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["service"].Should().Be("myservice");
    }

    [Fact]
    public void TryMatch_PartialMatch_Matches()
    {
        // Arrange
        var rule = CreateRule(@"v(?<version>\d+\.\d+\.\d+)");
        var message = CreateMessage("Deploying service to production v2.5.1");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["version"].Should().Be("2.5.1");
    }

    [Fact]
    public void TryMatch_MultipleCaptures_CapturesAll()
    {
        // Arrange
        var rule = CreateRule(@"(?<action>build|deploy|test)\s+(?<target>\S+)");
        var message = CreateMessage("build my-project");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["action"].Should().Be("build");
        result["target"].Should().Be("my-project");
    }

    [Fact]
    public void TryMatch_NullPattern_ReturnsNull()
    {
        // Arrange
        var rule = new TriggerRule
        {
            Name = "test",
            Sources = [TriggerSource.Telegram],
            Type = TriggerType.Pattern,
            Pattern = null,
            WorkflowPath = "/test.yaml"
        };
        var message = CreateMessage("anything");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    private static TriggerRule CreateRule(string pattern) => new()
    {
        Name = "test-rule",
        Sources = [TriggerSource.Telegram],
        Type = TriggerType.Pattern,
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
