namespace WorkflowEngine.Core.Models;

/// <summary>
/// Payload for a webhook notification.
/// </summary>
public sealed class WebhookNotification
{
    /// <summary>
    /// Gets the type of event that triggered this notification.
    /// </summary>
    public required WebhookEventType EventType { get; init; }

    /// <summary>
    /// Gets when the event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the workflow ID.
    /// </summary>
    public required string WorkflowId { get; init; }

    /// <summary>
    /// Gets the workflow run ID.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Gets the workflow name.
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// Gets the task ID if this is a task-level event.
    /// </summary>
    public string? TaskId { get; init; }

    /// <summary>
    /// Gets the task name if this is a task-level event.
    /// </summary>
    public string? TaskName { get; init; }

    /// <summary>
    /// Gets the execution status.
    /// </summary>
    public ExecutionStatus? Status { get; init; }

    /// <summary>
    /// Gets the exit code if applicable.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the error message if the event represents a failure.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the number of succeeded tasks (for workflow events).
    /// </summary>
    public int? SucceededTasks { get; init; }

    /// <summary>
    /// Gets the number of failed tasks (for workflow events).
    /// </summary>
    public int? FailedTasks { get; init; }

    /// <summary>
    /// Gets the number of skipped tasks (for workflow events).
    /// </summary>
    public int? SkippedTasks { get; init; }

    /// <summary>
    /// Gets the total number of tasks (for workflow events).
    /// </summary>
    public int? TotalTasks { get; init; }

    /// <summary>
    /// Gets whether the event represents a success state.
    /// </summary>
    public bool IsSuccess => EventType is WebhookEventType.WorkflowCompleted
        or WebhookEventType.TaskCompleted;

    /// <summary>
    /// Gets whether the event represents a failure state.
    /// </summary>
    public bool IsFailure => EventType is WebhookEventType.WorkflowFailed
        or WebhookEventType.TaskFailed
        or WebhookEventType.TaskTimedOut;

    /// <summary>
    /// Gets a human-readable summary of the event.
    /// </summary>
    public string Summary => EventType switch
    {
        WebhookEventType.WorkflowStarted => $"Workflow '{WorkflowName}' started ({TotalTasks} tasks)",
        WebhookEventType.WorkflowCompleted => $"Workflow '{WorkflowName}' completed in {Duration?.TotalSeconds:F1}s",
        WebhookEventType.WorkflowFailed => $"Workflow '{WorkflowName}' failed: {ErrorMessage}",
        WebhookEventType.WorkflowCancelled => $"Workflow '{WorkflowName}' was cancelled",
        WebhookEventType.TaskStarted => $"Task '{TaskId}' started",
        WebhookEventType.TaskCompleted => $"Task '{TaskId}' completed in {Duration?.TotalSeconds:F1}s",
        WebhookEventType.TaskFailed => $"Task '{TaskId}' failed: {ErrorMessage}",
        WebhookEventType.TaskSkipped => $"Task '{TaskId}' was skipped",
        WebhookEventType.TaskTimedOut => $"Task '{TaskId}' timed out",
        _ => $"Event {EventType} for workflow '{WorkflowName}'"
    };

    /// <inheritdoc />
    public override string ToString() => Summary;
}
