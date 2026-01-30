namespace WorkflowEngine.Core.Models;

/// <summary>
/// Configuration for a webhook notification endpoint.
/// </summary>
public sealed class WebhookConfig
{
    /// <summary>
    /// Gets the provider type (discord, slack, telegram, http).
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets the webhook URL to send notifications to.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets an optional name for this webhook configuration.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the events that should trigger this webhook.
    /// </summary>
    public IReadOnlyList<WebhookEventType> Events { get; init; } =
        [WebhookEventType.WorkflowCompleted, WebhookEventType.WorkflowFailed];

    /// <summary>
    /// Gets additional HTTP headers to include in webhook requests.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets provider-specific options (e.g., chat_id for Telegram).
    /// </summary>
    public IReadOnlyDictionary<string, string> Options { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the timeout in milliseconds for webhook requests.
    /// </summary>
    public int TimeoutMs { get; init; } = 10000;

    /// <summary>
    /// Gets the number of retry attempts on failure.
    /// </summary>
    public int RetryCount { get; init; } = 2;

    /// <inheritdoc />
    public override string ToString() =>
        $"Webhook[{Provider}] {Name ?? Url} ({Events.Count} events)";
}
