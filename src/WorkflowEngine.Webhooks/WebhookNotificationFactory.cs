using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;

namespace WorkflowEngine.Webhooks;

/// <summary>
/// Factory for creating webhook notifications from workflow and task events.
/// </summary>
public static class WebhookNotificationFactory
{
    /// <summary>
    /// Creates a webhook notification from a workflow event.
    /// </summary>
    /// <param name="evt">The workflow event.</param>
    /// <param name="eventType">The webhook event type.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <returns>The webhook notification.</returns>
    public static WebhookNotification Create(
        WorkflowEvent evt,
        WebhookEventType eventType,
        string workflowName)
    {
        return evt switch
        {
            WorkflowStartedEvent started => CreateFromStarted(started, eventType, workflowName),
            WorkflowCompletedEvent completed => CreateFromCompleted(completed, eventType, workflowName),
            WorkflowCancelledEvent cancelled => CreateFromCancelled(cancelled, eventType, workflowName),
            _ => CreateDefault(evt, eventType, workflowName)
        };
    }

    /// <summary>
    /// Creates a webhook notification from a task event.
    /// </summary>
    /// <param name="evt">The task event.</param>
    /// <param name="eventType">The webhook event type.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <returns>The webhook notification.</returns>
    public static WebhookNotification Create(
        TaskEvent evt,
        WebhookEventType eventType,
        string workflowName)
    {
        return evt switch
        {
            TaskStartedEvent started => CreateFromStarted(started, eventType, workflowName),
            TaskCompletedEvent completed => CreateFromCompleted(completed, eventType, workflowName),
            TaskSkippedEvent skipped => CreateFromSkipped(skipped, eventType, workflowName),
            _ => CreateDefaultTask(evt, eventType, workflowName)
        };
    }

    private static WebhookNotification CreateFromStarted(
        WorkflowStartedEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        TotalTasks = evt.TotalTasks
    };

    private static WebhookNotification CreateFromCompleted(
        WorkflowCompletedEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        Status = evt.Status,
        Duration = evt.Duration,
        SucceededTasks = evt.SucceededTasks,
        FailedTasks = evt.FailedTasks,
        SkippedTasks = evt.SkippedTasks,
        TotalTasks = evt.TotalTasks,
        ErrorMessage = evt.Status == ExecutionStatus.Failed ? $"{evt.FailedTasks} task(s) failed" : null
    };

    private static WebhookNotification CreateFromCancelled(
        WorkflowCancelledEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        ErrorMessage = evt.Reason
    };

    private static WebhookNotification CreateDefault(
        WorkflowEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName
    };

    private static WebhookNotification CreateFromStarted(
        TaskStartedEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        TaskId = evt.TaskId,
        TaskName = evt.TaskName
    };

    private static WebhookNotification CreateFromCompleted(
        TaskCompletedEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        TaskId = evt.TaskId,
        Status = evt.Status,
        ExitCode = evt.ExitCode,
        Duration = evt.Duration
    };

    private static WebhookNotification CreateFromSkipped(
        TaskSkippedEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        TaskId = evt.TaskId,
        ErrorMessage = evt.Reason
    };

    private static WebhookNotification CreateDefaultTask(
        TaskEvent evt,
        WebhookEventType eventType,
        string workflowName) => new()
    {
        EventType = eventType,
        Timestamp = evt.Timestamp,
        WorkflowId = evt.WorkflowId,
        RunId = evt.RunId,
        WorkflowName = workflowName,
        TaskId = evt.TaskId
    };
}
