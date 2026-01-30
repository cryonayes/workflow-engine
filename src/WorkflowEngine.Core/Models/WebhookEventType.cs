namespace WorkflowEngine.Core.Models;

/// <summary>
/// Types of events that can trigger webhook notifications.
/// </summary>
public enum WebhookEventType
{
    /// <summary>
    /// Fired when a workflow starts execution.
    /// </summary>
    WorkflowStarted,

    /// <summary>
    /// Fired when a workflow completes successfully.
    /// </summary>
    WorkflowCompleted,

    /// <summary>
    /// Fired when a workflow fails.
    /// </summary>
    WorkflowFailed,

    /// <summary>
    /// Fired when a workflow is cancelled.
    /// </summary>
    WorkflowCancelled,

    /// <summary>
    /// Fired when a task starts execution.
    /// </summary>
    TaskStarted,

    /// <summary>
    /// Fired when a task completes successfully.
    /// </summary>
    TaskCompleted,

    /// <summary>
    /// Fired when a task fails.
    /// </summary>
    TaskFailed,

    /// <summary>
    /// Fired when a task is skipped.
    /// </summary>
    TaskSkipped,

    /// <summary>
    /// Fired when a task times out.
    /// </summary>
    TaskTimedOut
}
