using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Webhooks.Providers;

/// <summary>
/// Webhook provider for Slack with Block Kit formatting.
/// </summary>
public sealed class SlackWebhookProvider : BaseWebhookProvider
{
    /// <inheritdoc />
    public override string ProviderType => "slack";

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    /// <param name="httpClient">The HTTP client for sending requests.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public SlackWebhookProvider(HttpClient httpClient, ILogger<SlackWebhookProvider> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    protected override object BuildPayload(WebhookConfig config, WebhookNotification notification)
    {
        var blocks = new List<object>
        {
            new SlackHeaderBlock
            {
                Text = new SlackTextObject
                {
                    Type = "plain_text",
                    Text = $"{GetEmojiForEvent(notification.EventType)} {GetEventTitle(notification.EventType)}",
                    Emoji = true
                }
            },
            new SlackSectionBlock
            {
                Text = new SlackTextObject
                {
                    Type = "mrkdwn",
                    Text = notification.Summary
                }
            }
        };

        // Add context fields
        var contextElements = new List<object>
        {
            new SlackTextObject
            {
                Type = "mrkdwn",
                Text = $"*Workflow:* {notification.WorkflowName}"
            }
        };

        if (!string.IsNullOrEmpty(notification.TaskId))
        {
            contextElements.Add(new SlackTextObject
            {
                Type = "mrkdwn",
                Text = $"*Task:* {notification.TaskName ?? notification.TaskId}"
            });
        }

        if (notification.Duration.HasValue)
        {
            contextElements.Add(new SlackTextObject
            {
                Type = "mrkdwn",
                Text = $"*Duration:* {FormatDuration(notification.Duration.Value)}"
            });
        }

        blocks.Add(new SlackContextBlock { Elements = contextElements });

        // Add stats section for workflow events
        if (notification.TotalTasks.HasValue)
        {
            blocks.Add(new SlackSectionBlock
            {
                Fields =
                [
                    new SlackTextObject { Type = "mrkdwn", Text = $"*Succeeded:* {notification.SucceededTasks ?? 0}" },
                    new SlackTextObject { Type = "mrkdwn", Text = $"*Failed:* {notification.FailedTasks ?? 0}" },
                    new SlackTextObject { Type = "mrkdwn", Text = $"*Skipped:* {notification.SkippedTasks ?? 0}" },
                    new SlackTextObject { Type = "mrkdwn", Text = $"*Total:* {notification.TotalTasks}" }
                ]
            });
        }

        // Add error section if present
        if (!string.IsNullOrEmpty(notification.ErrorMessage))
        {
            blocks.Add(new SlackDividerBlock());
            blocks.Add(new SlackSectionBlock
            {
                Text = new SlackTextObject
                {
                    Type = "mrkdwn",
                    Text = $"*Error:*\n```{TruncateText(notification.ErrorMessage, 2900)}```"
                }
            });
        }

        // Add footer
        blocks.Add(new SlackContextBlock
        {
            Elements =
            [
                new SlackTextObject
                {
                    Type = "mrkdwn",
                    Text = $"Run ID: `{notification.RunId[..Math.Min(8, notification.RunId.Length)]}` | {notification.Timestamp:yyyy-MM-dd HH:mm:ss} UTC"
                }
            ]
        });

        return new SlackMessage
        {
            Blocks = blocks,
            Text = notification.Summary // Fallback text for notifications
        };
    }

    #region Slack Block Kit Types
    private sealed class SlackMessage
    {
        public string? Text { get; init; }
        public List<object> Blocks { get; init; } = [];
    }

    private sealed class SlackHeaderBlock
    {
        public string Type => "header";
        public SlackTextObject? Text { get; init; }
    }

    private sealed class SlackSectionBlock
    {
        public string Type => "section";
        public SlackTextObject? Text { get; init; }
        public List<SlackTextObject>? Fields { get; init; }
    }

    private sealed class SlackContextBlock
    {
        public string Type => "context";
        public List<object> Elements { get; init; } = [];
    }

    private sealed class SlackDividerBlock
    {
        public string Type => "divider";
    }

    private sealed class SlackTextObject
    {
        public string? Type { get; init; }
        public string? Text { get; init; }
        public bool Emoji { get; init; }
    }

    #endregion
}
