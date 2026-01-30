using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Events;

/// <summary>
/// Base class for all workflow-related events.
/// </summary>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="RunId">The unique identifier for this workflow run.</param>
/// <param name="Timestamp">When the event occurred.</param>
public abstract record WorkflowEvent(
    string WorkflowId,
    string RunId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Raised when a workflow starts execution.
/// </summary>
public sealed record WorkflowStartedEvent(
    string WorkflowId,
    string RunId,
    string WorkflowName,
    int TotalTasks
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Workflow '{WorkflowName}' started ({TotalTasks} tasks)";
}

/// <summary>
/// Raised when a workflow completes (success or failure).
/// </summary>
public sealed record WorkflowCompletedEvent(
    string WorkflowId,
    string RunId,
    ExecutionStatus Status,
    TimeSpan Duration,
    int SucceededTasks,
    int FailedTasks,
    int SkippedTasks
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <summary>
    /// Gets the total number of tasks that were executed or skipped.
    /// </summary>
    public int TotalTasks => SucceededTasks + FailedTasks + SkippedTasks;

    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Workflow completed: {Status} " +
        $"(succeeded: {SucceededTasks}, failed: {FailedTasks}, skipped: {SkippedTasks}, " +
        $"duration: {Duration.TotalSeconds:F2}s)";
}

/// <summary>
/// Raised when a workflow is cancelled.
/// </summary>
public sealed record WorkflowCancelledEvent(
    string WorkflowId,
    string RunId,
    string Reason
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Workflow cancelled: {Reason}";
}

/// <summary>
/// Raised when an execution wave starts.
/// </summary>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="RunId">The unique identifier for this workflow run.</param>
/// <param name="WaveIndex">The zero-based index of the wave.</param>
/// <param name="TotalWaves">The total number of waves in the execution plan.</param>
/// <param name="TaskIds">The IDs of tasks in this wave.</param>
public sealed record WaveStartedEvent(
    string WorkflowId,
    string RunId,
    int WaveIndex,
    int TotalWaves,
    IReadOnlyList<string> TaskIds
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Wave {WaveIndex + 1}/{TotalWaves} started ({TaskIds.Count} tasks)";
}

/// <summary>
/// Raised when an execution wave completes.
/// </summary>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="RunId">The unique identifier for this workflow run.</param>
/// <param name="WaveIndex">The zero-based index of the wave.</param>
/// <param name="SucceededCount">Number of tasks that succeeded in this wave.</param>
/// <param name="FailedCount">Number of tasks that failed in this wave.</param>
/// <param name="SkippedCount">Number of tasks that were skipped in this wave.</param>
public sealed record WaveCompletedEvent(
    string WorkflowId,
    string RunId,
    int WaveIndex,
    int SucceededCount,
    int FailedCount,
    int SkippedCount
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Wave {WaveIndex + 1} completed " +
        $"(succeeded: {SucceededCount}, failed: {FailedCount}, skipped: {SkippedCount})";
}

/// <summary>
/// Raised when workflow execution is paused in step mode waiting for user input.
/// </summary>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="RunId">The unique identifier for this workflow run.</param>
/// <param name="CompletedTaskId">The ID of the task that just completed, or empty if waiting to start.</param>
/// <param name="CompletedTasks">Number of tasks completed so far.</param>
/// <param name="TotalTasks">Total number of tasks in the workflow.</param>
public sealed record StepPausedEvent(
    string WorkflowId,
    string RunId,
    string CompletedTaskId,
    int CompletedTasks,
    int TotalTasks
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <summary>
    /// Gets whether this pause is waiting to start the first task.
    /// </summary>
    public bool IsWaitingToStart => string.IsNullOrEmpty(CompletedTaskId);

    /// <inheritdoc />
    public override string ToString() => IsWaitingToStart
        ? $"[{Timestamp:HH:mm:ss}] Step mode: waiting to start ({TotalTasks} tasks)"
        : $"[{Timestamp:HH:mm:ss}] Step mode: paused after task '{CompletedTaskId}' ({CompletedTasks}/{TotalTasks})";
}

/// <summary>
/// Raised when workflow execution resumes after being paused in step mode.
/// </summary>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="RunId">The unique identifier for this workflow run.</param>
public sealed record StepResumedEvent(
    string WorkflowId,
    string RunId
) : WorkflowEvent(WorkflowId, RunId, DateTimeOffset.UtcNow)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] Step mode: resumed execution";
}
