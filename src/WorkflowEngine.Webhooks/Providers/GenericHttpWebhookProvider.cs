using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Webhooks.Providers;

/// <summary>
/// Generic HTTP webhook provider that sends raw JSON payloads.
/// </summary>
public sealed class GenericHttpWebhookProvider : BaseWebhookProvider
{
    /// <inheritdoc />
    public override string ProviderType => "http";

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    /// <param name="httpClient">The HTTP client for sending requests.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public GenericHttpWebhookProvider(HttpClient httpClient, ILogger<GenericHttpWebhookProvider> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    protected override object BuildPayload(WebhookConfig config, WebhookNotification notification)
    {
        return new GenericWebhookPayload
        {
            EventType = notification.EventType.ToString(),
            Timestamp = notification.Timestamp,
            WorkflowId = notification.WorkflowId,
            RunId = notification.RunId,
            WorkflowName = notification.WorkflowName,
            TaskId = notification.TaskId,
            TaskName = notification.TaskName,
            Status = notification.Status?.ToString(),
            ExitCode = notification.ExitCode,
            DurationMs = notification.Duration?.TotalMilliseconds,
            ErrorMessage = notification.ErrorMessage,
            SucceededTasks = notification.SucceededTasks,
            FailedTasks = notification.FailedTasks,
            SkippedTasks = notification.SkippedTasks,
            TotalTasks = notification.TotalTasks,
            Summary = notification.Summary
        };
    }

    // Generic payload type with all notification fields
    private sealed class GenericWebhookPayload
    {
        public string? EventType { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public string? WorkflowId { get; init; }
        public string? RunId { get; init; }
        public string? WorkflowName { get; init; }
        public string? TaskId { get; init; }
        public string? TaskName { get; init; }
        public string? Status { get; init; }
        public int? ExitCode { get; init; }
        public double? DurationMs { get; init; }
        public string? ErrorMessage { get; init; }
        public int? SucceededTasks { get; init; }
        public int? FailedTasks { get; init; }
        public int? SkippedTasks { get; init; }
        public int? TotalTasks { get; init; }
        public string? Summary { get; init; }
    }
}
