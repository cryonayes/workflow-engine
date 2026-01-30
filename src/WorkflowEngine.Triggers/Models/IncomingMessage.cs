namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Represents a normalized incoming message from any trigger source.
/// </summary>
public sealed class IncomingMessage
{
    /// <summary>
    /// Gets the unique message identifier from the source platform.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the source platform of the message.
    /// </summary>
    public required TriggerSource Source { get; init; }

    /// <summary>
    /// Gets the text content of the message.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the username of the message sender.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the user ID of the message sender.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the channel or chat ID where the message was sent.
    /// </summary>
    public string? ChannelId { get; init; }

    /// <summary>
    /// Gets the channel or chat name.
    /// </summary>
    public string? ChannelName { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets additional metadata from the source platform.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the raw payload from the source platform for reply handling.
    /// </summary>
    public object? RawPayload { get; init; }

    /// <summary>
    /// Gets the sender display name, preferring username over userId.
    /// </summary>
    public string SenderDisplayName => Username ?? UserId ?? "unknown";

    /// <inheritdoc />
    public override string ToString() => $"[{Source}] {SenderDisplayName}: {Text}";
}

/// <summary>
/// Factory methods for creating IncomingMessage instances.
/// </summary>
public static class IncomingMessageFactory
{
    /// <summary>
    /// Creates an IncomingMessage from a Telegram update.
    /// </summary>
    public static IncomingMessage FromTelegram(
        string messageId,
        string text,
        string? username,
        string? userId,
        string? chatId,
        object? rawPayload = null) => new()
    {
        MessageId = messageId,
        Source = TriggerSource.Telegram,
        Text = text,
        Username = username,
        UserId = userId,
        ChannelId = chatId,
        RawPayload = rawPayload
    };

    /// <summary>
    /// Creates an IncomingMessage from a Discord message.
    /// </summary>
    public static IncomingMessage FromDiscord(
        string messageId,
        string text,
        string? username,
        string? userId,
        string? channelId,
        string? channelName,
        object? rawPayload = null) => new()
    {
        MessageId = messageId,
        Source = TriggerSource.Discord,
        Text = text,
        Username = username,
        UserId = userId,
        ChannelId = channelId,
        ChannelName = channelName,
        RawPayload = rawPayload
    };

    /// <summary>
    /// Creates an IncomingMessage from a Slack event.
    /// </summary>
    public static IncomingMessage FromSlack(
        string messageId,
        string text,
        string? username,
        string? userId,
        string? channelId,
        string? channelName,
        object? rawPayload = null) => new()
    {
        MessageId = messageId,
        Source = TriggerSource.Slack,
        Text = text,
        Username = username,
        UserId = userId,
        ChannelId = channelId,
        ChannelName = channelName,
        RawPayload = rawPayload
    };

    /// <summary>
    /// Creates an IncomingMessage from an HTTP webhook request.
    /// </summary>
    public static IncomingMessage FromHttp(
        string messageId,
        string text,
        IReadOnlyDictionary<string, string>? metadata = null,
        object? rawPayload = null) => new()
    {
        MessageId = messageId,
        Source = TriggerSource.Http,
        Text = text,
        Metadata = metadata ?? new Dictionary<string, string>(),
        RawPayload = rawPayload
    };

    /// <summary>
    /// Creates an IncomingMessage from a file watch event.
    /// </summary>
    /// <param name="filePath">The path of the file that changed.</param>
    /// <param name="changeType">The type of change (Created, Changed, Renamed, Deleted).</param>
    /// <param name="metadata">Optional additional metadata.</param>
    public static IncomingMessage FromFileWatch(
        string filePath,
        string changeType,
        IReadOnlyDictionary<string, string>? metadata = null) => new()
    {
        MessageId = $"filewatch-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Path.GetFileName(filePath)}",
        Source = TriggerSource.FileWatch,
        Text = $"{changeType}: {filePath}",
        Metadata = metadata ?? new Dictionary<string, string>
        {
            ["filePath"] = filePath,
            ["changeType"] = changeType,
            ["fileName"] = Path.GetFileName(filePath),
            ["directory"] = Path.GetDirectoryName(filePath) ?? string.Empty
        }
    };
}
