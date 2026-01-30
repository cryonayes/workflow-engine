using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Webhooks.Providers;

/// <summary>
/// Webhook provider for Discord with rich embed formatting.
/// </summary>
public sealed class DiscordWebhookProvider : BaseWebhookProvider
{
    /// <inheritdoc />
    public override string ProviderType => "discord";

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    /// <param name="httpClient">The HTTP client for sending requests.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public DiscordWebhookProvider(HttpClient httpClient, ILogger<DiscordWebhookProvider> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    protected override object BuildPayload(WebhookConfig config, WebhookNotification notification)
    {
        var embed = new DiscordEmbed
        {
            Title = $"{GetEmojiForEvent(notification.EventType)} {GetEventTitle(notification.EventType)}",
            Description = notification.Summary,
            Color = GetColorForEvent(notification.EventType),
            Timestamp = notification.Timestamp,
            Footer = new DiscordFooter
            {
                Text = $"Run: {notification.RunId[..Math.Min(8, notification.RunId.Length)]}"
            },
            Fields = BuildFields(notification)
        };

        return new DiscordMessage
        {
            Username = config.Name ?? "Workflow Engine",
            Embeds = [embed]
        };
    }

    private static List<DiscordField> BuildFields(WebhookNotification notification)
    {
        var fields = new List<DiscordField>
        {
            new() { Name = "Workflow", Value = notification.WorkflowName, Inline = true }
        };

        if (!string.IsNullOrEmpty(notification.TaskId))
        {
            fields.Add(new DiscordField
            {
                Name = "Task",
                Value = notification.TaskName ?? notification.TaskId,
                Inline = true
            });
        }

        if (notification.Duration.HasValue)
        {
            fields.Add(new DiscordField
            {
                Name = "Duration",
                Value = FormatDuration(notification.Duration.Value),
                Inline = true
            });
        }

        if (notification.TotalTasks.HasValue)
        {
            var statsValue = $"✓ {notification.SucceededTasks ?? 0} | ✗ {notification.FailedTasks ?? 0} | ⏭ {notification.SkippedTasks ?? 0}";
            fields.Add(new DiscordField
            {
                Name = $"Tasks ({notification.TotalTasks})",
                Value = statsValue,
                Inline = true
            });
        }

        if (!string.IsNullOrEmpty(notification.ErrorMessage))
        {
            fields.Add(new DiscordField
            {
                Name = "Error",
                Value = FormatError(notification.ErrorMessage, 1000, wrapInCodeBlock: true),
                Inline = false
            });
        }

        return fields;
    }

    #region Discord API Types

    private sealed class DiscordMessage
    {
        public string? Username { get; init; }
        public List<DiscordEmbed> Embeds { get; init; } = [];
    }

    private sealed class DiscordEmbed
    {
        public string? Title { get; init; }
        public string? Description { get; init; }
        public int Color { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public DiscordFooter? Footer { get; init; }
        public List<DiscordField> Fields { get; init; } = [];
    }

    private sealed class DiscordFooter
    {
        public string? Text { get; init; }
    }

    private sealed class DiscordField
    {
        public string? Name { get; init; }
        public string? Value { get; init; }
        public bool Inline { get; init; }
    }

    #endregion
}
