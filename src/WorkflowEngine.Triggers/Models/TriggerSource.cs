namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Represents the source platform for incoming trigger messages.
/// </summary>
public enum TriggerSource
{
    /// <summary>
    /// Telegram bot messages.
    /// </summary>
    Telegram,

    /// <summary>
    /// Discord bot messages.
    /// </summary>
    Discord,

    /// <summary>
    /// Slack app messages via Events API.
    /// </summary>
    Slack,

    /// <summary>
    /// Generic HTTP webhook requests.
    /// </summary>
    Http,

    /// <summary>
    /// File system watch events.
    /// </summary>
    FileWatch
}
