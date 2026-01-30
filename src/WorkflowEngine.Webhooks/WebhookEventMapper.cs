using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;

namespace WorkflowEngine.Webhooks;

/// <summary>
/// Maps workflow and task events to webhook event types.
/// </summary>
public static class WebhookEventMapper
{
    /// <summary>
    /// Maps a workflow event to a webhook event type.
    /// </summary>
    /// <param name="evt">The workflow event.</param>
    /// <returns>The webhook event type, or null if the event should not trigger a webhook.</returns>
    public static WebhookEventType? MapWorkflowEvent(WorkflowEvent evt) => evt switch
    {
        WorkflowStartedEvent => WebhookEventType.WorkflowStarted,
        WorkflowCompletedEvent completed when completed.Status == ExecutionStatus.Succeeded => WebhookEventType.WorkflowCompleted,
        WorkflowCompletedEvent => WebhookEventType.WorkflowFailed,
        WorkflowCancelledEvent => WebhookEventType.WorkflowCancelled,
        _ => null
    };

    /// <summary>
    /// Maps a task event to a webhook event type.
    /// </summary>
    /// <param name="evt">The task event.</param>
    /// <returns>The webhook event type, or null if the event should not trigger a webhook.</returns>
    public static WebhookEventType? MapTaskEvent(TaskEvent evt) => evt switch
    {
        TaskStartedEvent => WebhookEventType.TaskStarted,
        TaskCompletedEvent completed when completed.IsSuccess => WebhookEventType.TaskCompleted,
        TaskCompletedEvent completed when completed.Status == ExecutionStatus.TimedOut => WebhookEventType.TaskTimedOut,
        TaskCompletedEvent => WebhookEventType.TaskFailed,
        TaskSkippedEvent => WebhookEventType.TaskSkipped,
        _ => null
    };
}
