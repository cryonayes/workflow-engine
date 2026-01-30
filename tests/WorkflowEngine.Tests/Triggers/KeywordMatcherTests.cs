using FluentAssertions;
using WorkflowEngine.Triggers.Matching;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Tests.Triggers;

public class KeywordMatcherTests
{
    private readonly KeywordMatcher _matcher = new();

    [Fact]
    public void TryMatch_ExactKeyword_Matches()
    {
        // Arrange
        var rule = CreateRule("hotfix deploy", "emergency release");
        var message = CreateMessage("Please do a hotfix deploy");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["keyword"].Should().Be("hotfix deploy");
    }

    [Fact]
    public void TryMatch_CaseInsensitive_Matches()
    {
        // Arrange
        var rule = CreateRule("HOTFIX DEPLOY");
        var message = CreateMessage("need a hotfix deploy now");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["keyword"].Should().Be("HOTFIX DEPLOY");
    }

    [Fact]
    public void TryMatch_MultipleKeywords_MatchesFirst()
    {
        // Arrange
        var rule = CreateRule("emergency", "urgent", "critical");
        var message = CreateMessage("This is urgent and critical");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
        result!["keyword"].Should().Be("urgent");
    }

    [Fact]
    public void TryMatch_NoKeywordFound_ReturnsNull()
    {
        // Arrange
        var rule = CreateRule("hotfix", "emergency");
        var message = CreateMessage("regular deployment scheduled");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_EmptyKeywords_ReturnsNull()
    {
        // Arrange
        var rule = new TriggerRule
        {
            Name = "test",
            Sources = [TriggerSource.Telegram],
            Type = TriggerType.Keyword,
            Keywords = [],
            WorkflowPath = "/test.yaml"
        };
        var message = CreateMessage("anything");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_KeywordAtStart_Matches()
    {
        // Arrange
        var rule = CreateRule("deploy now");
        var message = CreateMessage("deploy now to production");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void TryMatch_KeywordAtEnd_Matches()
    {
        // Arrange
        var rule = CreateRule("deploy now");
        var message = CreateMessage("we need to deploy now");

        // Act
        var result = _matcher.TryMatch(message, rule);

        // Assert
        result.Should().NotBeNull();
    }

    private static TriggerRule CreateRule(params string[] keywords) => new()
    {
        Name = "test-rule",
        Sources = [TriggerSource.Telegram],
        Type = TriggerType.Keyword,
        Keywords = keywords,
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
