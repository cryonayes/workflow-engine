using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.TypeParsers;

/// <summary>
/// Parses string values into WebhookEventType enum values.
/// </summary>
public sealed class WebhookEventTypeParser : ITypeParser<WebhookEventType>
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static WebhookEventTypeParser Instance { get; } = new();

    private readonly Dictionary<string, WebhookEventType> _mappings;

    private WebhookEventTypeParser()
    {
        _mappings = new Dictionary<string, WebhookEventType>(StringComparer.OrdinalIgnoreCase)
        {
            ["workflowstarted"] = WebhookEventType.WorkflowStarted,
            ["workflow_started"] = WebhookEventType.WorkflowStarted,
            ["workflowcompleted"] = WebhookEventType.WorkflowCompleted,
            ["workflow_completed"] = WebhookEventType.WorkflowCompleted,
            ["workflowfailed"] = WebhookEventType.WorkflowFailed,
            ["workflow_failed"] = WebhookEventType.WorkflowFailed,
            ["workflowcancelled"] = WebhookEventType.WorkflowCancelled,
            ["workflow_cancelled"] = WebhookEventType.WorkflowCancelled,
            ["taskstarted"] = WebhookEventType.TaskStarted,
            ["task_started"] = WebhookEventType.TaskStarted,
            ["taskcompleted"] = WebhookEventType.TaskCompleted,
            ["task_completed"] = WebhookEventType.TaskCompleted,
            ["taskfailed"] = WebhookEventType.TaskFailed,
            ["task_failed"] = WebhookEventType.TaskFailed,
            ["taskskipped"] = WebhookEventType.TaskSkipped,
            ["task_skipped"] = WebhookEventType.TaskSkipped,
            ["tasktimedout"] = WebhookEventType.TaskTimedOut,
            ["task_timedout"] = WebhookEventType.TaskTimedOut,
            ["task_timed_out"] = WebhookEventType.TaskTimedOut
        };
    }

    /// <inheritdoc />
    public WebhookEventType Parse(string? value)
    {
        if (TryParse(value, out var result))
            return result;

        throw new WorkflowParsingException($"Unknown webhook event type: {value}");
    }

    /// <inheritdoc />
    public bool TryParse(string? value, out WebhookEventType result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Normalize: remove underscores and lowercase
        var normalized = value.Replace("_", "").ToLowerInvariant();
        return _mappings.TryGetValue(normalized, out result) ||
               _mappings.TryGetValue(value, out result);
    }
}
