using WorkflowEngine.Core.Extensions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Events;

/// <summary>
/// Base class for task-related events.
/// </summary>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="RunId">The unique identifier for this workflow run.</param>
/// <param name="TaskId">The task ID that generated this event.</param>
/// <param name="Timestamp">When the event occurred.</param>
public abstract record TaskEvent(
    string WorkflowId,
    string RunId,
    string TaskId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Raised when a task starts execution.
/// </summary>
public sealed record TaskStartedEvent(
    string WorkflowId,
    string RunId,
    string TaskId,
    string? TaskName,
    int TaskIndex,
    int TotalTasks
) : TaskEvent(WorkflowId, RunId, TaskId, DateTimeOffset.UtcNow)
{
    /// <summary>
    /// Gets the display name for the task.
    /// </summary>
    public string DisplayName => TaskName ?? TaskId;

    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Task '{DisplayName}' started ({TaskIndex}/{TotalTasks})";
}

/// <summary>
/// Raised when a task produces output (real-time streaming).
/// </summary>
public sealed record TaskOutputEvent(
    string WorkflowId,
    string RunId,
    string TaskId,
    string Line,
    OutputStreamType StreamType
) : TaskEvent(WorkflowId, RunId, TaskId, DateTimeOffset.UtcNow)
{
    /// <summary>
    /// Gets whether this output is from stderr.
    /// </summary>
    public bool IsError => StreamType == OutputStreamType.StdErr;

    /// <inheritdoc />
    public override string ToString() =>
        IsError
            ? $"[{Timestamp:HH:mm:ss}] [{TaskId}:stderr] {Line}"
            : $"[{Timestamp:HH:mm:ss}] [{TaskId}] {Line}";
}

/// <summary>
/// Raised when a task completes.
/// </summary>
public sealed record TaskCompletedEvent(
    string WorkflowId,
    string RunId,
    string TaskId,
    ExecutionStatus Status,
    int ExitCode,
    TimeSpan Duration
) : TaskEvent(WorkflowId, RunId, TaskId, DateTimeOffset.UtcNow)
{
    /// <summary>
    /// Gets whether the task succeeded.
    /// </summary>
    public bool IsSuccess => Status.IsSuccessful(ExitCode);

    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Task '{TaskId}' {Status} " +
        $"(exit: {ExitCode}, duration: {Duration.TotalSeconds:F2}s)";
}

/// <summary>
/// Raised when a task is skipped due to condition evaluation.
/// </summary>
public sealed record TaskSkippedEvent(
    string WorkflowId,
    string RunId,
    string TaskId,
    string Reason
) : TaskEvent(WorkflowId, RunId, TaskId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Task '{TaskId}' skipped: {Reason}";
}

/// <summary>
/// Raised when a task is cancelled.
/// </summary>
public sealed record TaskCancelledEvent(
    string WorkflowId,
    string RunId,
    string TaskId,
    string Reason,
    TimeSpan Duration
) : TaskEvent(WorkflowId, RunId, TaskId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Task '{TaskId}' cancelled: {Reason} (duration: {Duration.TotalSeconds:F2}s)";
}
