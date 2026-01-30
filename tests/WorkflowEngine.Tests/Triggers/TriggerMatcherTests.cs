using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Matching;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Tests.Triggers;

public class TriggerMatcherTests
{
    private readonly TriggerMatcher _matcher;

    public TriggerMatcherTests()
    {
        var typedMatchers = new ITypedTriggerMatcher[]
        {
            new CommandMatcher(),
            new PatternMatcher(),
            new KeywordMatcher()
        };

        _matcher = new TriggerMatcher(typedMatchers, NullLogger<TriggerMatcher>.Instance);
    }

    [Fact]
    public void Match_CommandRule_Matches()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "build",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Command,
                Pattern = "/build {project}",
                WorkflowPath = "/workflows/build.yaml"
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Telegram,
            Text = "/build my-api",
            Username = "john"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().NotBeNull();
        result!.IsMatch.Should().BeTrue();
        result.Rule!.Name.Should().Be("build");
        result.Captures["project"].Should().Be("my-api");
    }

    [Fact]
    public void Match_SkipsDisabledRules()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "disabled-rule",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Command,
                Pattern = "/test",
                WorkflowPath = "/workflows/test.yaml",
                Enabled = false
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Telegram,
            Text = "/test"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Match_SkipsWrongSource()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "telegram-only",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Command,
                Pattern = "/test",
                WorkflowPath = "/workflows/test.yaml"
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Discord,
            Text = "/test"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Match_FirstMatchWins()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "first-rule",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Command,
                Pattern = "/build {project}",
                WorkflowPath = "/workflows/first.yaml"
            },
            new()
            {
                Name = "second-rule",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Command,
                Pattern = "/build {name}",
                WorkflowPath = "/workflows/second.yaml"
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Telegram,
            Text = "/build my-api"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().NotBeNull();
        result!.Rule!.Name.Should().Be("first-rule");
    }

    [Fact]
    public void Match_MultipleSources_MatchesAny()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "multi-source",
                Sources = [TriggerSource.Telegram, TriggerSource.Discord, TriggerSource.Slack],
                Type = TriggerType.Command,
                Pattern = "/deploy",
                WorkflowPath = "/workflows/deploy.yaml"
            }
        };

        var telegramMessage = new IncomingMessage
        {
            MessageId = "1",
            Source = TriggerSource.Telegram,
            Text = "/deploy"
        };

        var discordMessage = new IncomingMessage
        {
            MessageId = "2",
            Source = TriggerSource.Discord,
            Text = "/deploy"
        };

        // Act & Assert
        _matcher.Match(telegramMessage, rules).Should().NotBeNull();
        _matcher.Match(discordMessage, rules).Should().NotBeNull();
    }

    [Fact]
    public void Match_KeywordRule_Matches()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "emergency",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Keyword,
                Keywords = ["hotfix", "emergency"],
                WorkflowPath = "/workflows/emergency.yaml"
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Telegram,
            Text = "We need a hotfix deployed now!"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().NotBeNull();
        result!.Rule!.Name.Should().Be("emergency");
        result.Captures["keyword"].Should().Be("hotfix");
    }

    [Fact]
    public void Match_PatternRule_Matches()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "version-deploy",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Pattern,
                Pattern = @"deploy\s+(?<service>[\w-]+)\s+v(?<version>[\d.]+)",
                WorkflowPath = "/workflows/deploy.yaml"
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Telegram,
            Text = "deploy auth-api v1.2.3"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().NotBeNull();
        result!.Captures["service"].Should().Be("auth-api");
        result.Captures["version"].Should().Be("1.2.3");
    }

    [Fact]
    public void Match_NoMatchingRules_ReturnsNull()
    {
        // Arrange
        var rules = new List<TriggerRule>
        {
            new()
            {
                Name = "build",
                Sources = [TriggerSource.Telegram],
                Type = TriggerType.Command,
                Pattern = "/build {project}",
                WorkflowPath = "/workflows/build.yaml"
            }
        };

        var message = new IncomingMessage
        {
            MessageId = "123",
            Source = TriggerSource.Telegram,
            Text = "hello world"
        };

        // Act
        var result = _matcher.Match(message, rules);

        // Assert
        result.Should().BeNull();
    }
}
