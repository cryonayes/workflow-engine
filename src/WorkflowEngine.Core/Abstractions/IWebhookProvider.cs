using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Provides webhook delivery for a specific provider type (Discord, Slack, etc.).
/// </summary>
public interface IWebhookProvider
{
    /// <summary>
    /// Gets the provider type identifier (e.g., "discord", "slack", "telegram", "http").
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Sends a webhook notification.
    /// </summary>
    /// <param name="config">The webhook configuration.</param>
    /// <param name="notification">The notification payload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the webhook delivery.</returns>
    Task<WebhookResult> SendAsync(
        WebhookConfig config,
        WebhookNotification notification,
        CancellationToken cancellationToken = default);
}
