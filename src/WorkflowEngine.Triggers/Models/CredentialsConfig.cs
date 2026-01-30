namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Represents credentials configuration for trigger sources.
/// </summary>
public sealed class CredentialsConfig
{
    /// <summary>
    /// Gets the Telegram bot credentials.
    /// </summary>
    public TelegramCredentials? Telegram { get; init; }

    /// <summary>
    /// Gets the Discord bot credentials.
    /// </summary>
    public DiscordCredentials? Discord { get; init; }

    /// <summary>
    /// Gets the Slack app credentials.
    /// </summary>
    public SlackCredentials? Slack { get; init; }
}

/// <summary>
/// Telegram bot credentials.
/// </summary>
public sealed class TelegramCredentials
{
    /// <summary>
    /// Gets the bot token from BotFather.
    /// </summary>
    public required string BotToken { get; init; }
}

/// <summary>
/// Discord bot credentials.
/// </summary>
public sealed class DiscordCredentials
{
    /// <summary>
    /// Gets the bot token from Discord Developer Portal.
    /// </summary>
    public required string BotToken { get; init; }
}

/// <summary>
/// Slack app credentials.
/// </summary>
public sealed class SlackCredentials
{
    /// <summary>
    /// Gets the app-level token for Socket Mode.
    /// </summary>
    public required string AppToken { get; init; }

    /// <summary>
    /// Gets the signing secret for request verification.
    /// </summary>
    public required string SigningSecret { get; init; }
}
