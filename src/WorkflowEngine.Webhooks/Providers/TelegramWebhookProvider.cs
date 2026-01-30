using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Webhooks.Providers;

/// <summary>
/// Webhook provider for Telegram Bot API with HTML formatting.
/// </summary>
public sealed class TelegramWebhookProvider : BaseWebhookProvider
{
    /// <inheritdoc />
    public override string ProviderType => "telegram";

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    /// <param name="httpClient">The HTTP client for sending requests.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public TelegramWebhookProvider(HttpClient httpClient, ILogger<TelegramWebhookProvider> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    protected override object BuildPayload(WebhookConfig config, WebhookNotification notification)
    {
        // Get chat_id from options (required for Telegram)
        if (!config.Options.TryGetValue("chat_id", out var chatId) || string.IsNullOrEmpty(chatId))
        {
            throw new InvalidOperationException("Telegram webhook requires 'chat_id' in options");
        }

        var message = BuildHtmlMessage(notification);

        return new TelegramSendMessage
        {
            ChatId = chatId,
            Text = message,
            ParseMode = "HTML",
            DisableWebPagePreview = true
        };
    }

    private string BuildHtmlMessage(WebhookNotification notification)
    {
        var sb = new StringBuilder();

        // Header with emoji
        var emoji = GetEmojiForEvent(notification.EventType);
        var title = GetEventTitle(notification.EventType);
        sb.AppendLine($"{emoji} <b>{HttpUtility.HtmlEncode(title)}</b>");
        sb.AppendLine();

        // Summary
        sb.AppendLine(HttpUtility.HtmlEncode(notification.Summary));
        sb.AppendLine();

        // Details section
        sb.AppendLine("<b>Details:</b>");
        sb.AppendLine($"• Workflow: <code>{HttpUtility.HtmlEncode(notification.WorkflowName)}</code>");

        if (!string.IsNullOrEmpty(notification.TaskId))
        {
            var taskDisplay = notification.TaskName ?? notification.TaskId;
            sb.AppendLine($"• Task: <code>{HttpUtility.HtmlEncode(taskDisplay)}</code>");
        }

        if (notification.Duration.HasValue)
        {
            sb.AppendLine($"• Duration: {FormatDuration(notification.Duration.Value)}");
        }

        // Stats for workflow events
        if (notification.TotalTasks.HasValue)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Task Results:</b>");
            sb.AppendLine($"✓ Succeeded: {notification.SucceededTasks ?? 0}");
            sb.AppendLine($"✗ Failed: {notification.FailedTasks ?? 0}");
            sb.AppendLine($"⏭ Skipped: {notification.SkippedTasks ?? 0}");
        }

        // Error message if present
        if (!string.IsNullOrEmpty(notification.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine("<b>Error:</b>");
            sb.AppendLine($"<pre>{HttpUtility.HtmlEncode(TruncateText(notification.ErrorMessage, 3000))}</pre>");
        }

        // Footer
        sb.AppendLine();
        sb.AppendLine($"<i>Run: {notification.RunId[..Math.Min(8, notification.RunId.Length)]}</i>");

        return sb.ToString();
    }

    #region Telegram API Types

    private sealed class TelegramSendMessage
    {
        public string? ChatId { get; init; }
        public string? Text { get; init; }
        public string? ParseMode { get; init; }
        public bool DisableWebPagePreview { get; init; }
    }

    #endregion
}
