using FluentAssertions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Tests.Triggers;

public class IncomingMessageTests
{
    [Fact]
    public void FromTelegram_CreatesCorrectMessage()
    {
        // Act
        var message = IncomingMessageFactory.FromTelegram(
            messageId: "12345",
            text: "Hello, world!",
            username: "john_doe",
            userId: "67890",
            chatId: "chat-123");

        // Assert
        message.Source.Should().Be(TriggerSource.Telegram);
        message.MessageId.Should().Be("12345");
        message.Text.Should().Be("Hello, world!");
        message.Username.Should().Be("john_doe");
        message.UserId.Should().Be("67890");
        message.ChannelId.Should().Be("chat-123");
    }

    [Fact]
    public void FromDiscord_CreatesCorrectMessage()
    {
        // Act
        var message = IncomingMessageFactory.FromDiscord(
            messageId: "msg-123",
            text: "Discord message",
            username: "user#1234",
            userId: "user-456",
            channelId: "channel-789",
            channelName: "general");

        // Assert
        message.Source.Should().Be(TriggerSource.Discord);
        message.MessageId.Should().Be("msg-123");
        message.Text.Should().Be("Discord message");
        message.Username.Should().Be("user#1234");
        message.ChannelId.Should().Be("channel-789");
        message.ChannelName.Should().Be("general");
    }

    [Fact]
    public void FromSlack_CreatesCorrectMessage()
    {
        // Act
        var message = IncomingMessageFactory.FromSlack(
            messageId: "ts-123",
            text: "Slack message",
            username: null,
            userId: "U12345",
            channelId: "C67890",
            channelName: "random");

        // Assert
        message.Source.Should().Be(TriggerSource.Slack);
        message.MessageId.Should().Be("ts-123");
        message.UserId.Should().Be("U12345");
        message.ChannelName.Should().Be("random");
    }

    [Fact]
    public void FromHttp_CreatesCorrectMessage()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["header:X-Custom"] = "value",
            ["query:param"] = "test"
        };

        // Act
        var message = IncomingMessageFactory.FromHttp(
            messageId: "webhook-123",
            text: "Webhook payload",
            metadata: metadata);

        // Assert
        message.Source.Should().Be(TriggerSource.Http);
        message.MessageId.Should().Be("webhook-123");
        message.Text.Should().Be("Webhook payload");
        message.Metadata.Should().ContainKey("header:X-Custom");
        message.Metadata["header:X-Custom"].Should().Be("value");
    }

    [Fact]
    public void SenderDisplayName_PrefersUsername()
    {
        // Arrange
        var message = IncomingMessageFactory.FromTelegram(
            messageId: "123",
            text: "Test",
            username: "alice",
            userId: "456",
            chatId: "789");

        // Assert
        message.SenderDisplayName.Should().Be("alice");
    }

    [Fact]
    public void SenderDisplayName_FallsBackToUserId()
    {
        // Arrange
        var message = IncomingMessageFactory.FromTelegram(
            messageId: "123",
            text: "Test",
            username: null,
            userId: "456",
            chatId: "789");

        // Assert
        message.SenderDisplayName.Should().Be("456");
    }

    [Fact]
    public void SenderDisplayName_FallsBackToUnknown()
    {
        // Arrange
        var message = IncomingMessageFactory.FromHttp(
            messageId: "123",
            text: "Test");

        // Assert
        message.SenderDisplayName.Should().Be("unknown");
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var message = IncomingMessageFactory.FromTelegram(
            messageId: "123",
            text: "Test message",
            username: "alice",
            userId: "456",
            chatId: "789");

        // Act
        var result = message.ToString();

        // Assert
        result.Should().Contain("Telegram");
        result.Should().Contain("alice");
        result.Should().Contain("Test message");
    }

    [Fact]
    public void ReceivedAt_IsSet()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var message = IncomingMessageFactory.FromTelegram(
            messageId: "123",
            text: "Test",
            username: null,
            userId: null,
            chatId: null);

        var after = DateTimeOffset.UtcNow;

        // Assert
        message.ReceivedAt.Should().BeOnOrAfter(before);
        message.ReceivedAt.Should().BeOnOrBefore(after);
    }
}
