using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Events;

/// <summary>
/// Helper class for publishing task-related events.
/// Eliminates duplication between WaveExecutor and TaskRetrier.
/// </summary>
public sealed class TaskEventHelper
{
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// Creates a new TaskEventHelper instance.
    /// </summary>
    public TaskEventHelper(IEventPublisher eventPublisher)
    {
        ArgumentNullException.ThrowIfNull(eventPublisher);
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Publishes a TaskStartedEvent.
    /// </summary>
    public void PublishTaskStarted(
        string workflowId,
        string runId,
        string taskId,
        string? taskName,
        int taskIndex,
        int totalTasks)
    {
        _eventPublisher.PublishTaskEvent(new TaskStartedEvent(
            workflowId,
            runId,
            taskId,
            taskName,
            taskIndex,
            totalTasks));
    }

    /// <summary>
    /// Publishes a TaskStartedEvent using context.
    /// </summary>
    public void PublishTaskStarted(
        WorkflowContext context,
        WorkflowTask task,
        int taskIndex,
        int totalTasks)
    {
        PublishTaskStarted(
            context.Workflow.Id,
            context.RunId,
            task.Id,
            task.Name,
            taskIndex,
            totalTasks);
    }

    /// <summary>
    /// Publishes a TaskOutputEvent.
    /// </summary>
    public void PublishTaskOutput(
        string workflowId,
        string runId,
        string taskId,
        string message,
        OutputStreamType streamType)
    {
        _eventPublisher.PublishTaskEvent(new TaskOutputEvent(
            workflowId,
            runId,
            taskId,
            message,
            streamType));
    }

    /// <summary>
    /// Publishes a TaskCompletedEvent.
    /// </summary>
    public void PublishTaskCompleted(
        string workflowId,
        string runId,
        string taskId,
        ExecutionStatus status,
        int? exitCode,
        TimeSpan duration)
    {
        _eventPublisher.PublishTaskEvent(new TaskCompletedEvent(
            workflowId,
            runId,
            taskId,
            status,
            exitCode ?? 0,
            duration));
    }

    /// <summary>
    /// Publishes a TaskSkippedEvent.
    /// </summary>
    public void PublishTaskSkipped(
        string workflowId,
        string runId,
        string taskId,
        string reason)
    {
        _eventPublisher.PublishTaskEvent(new TaskSkippedEvent(
            workflowId,
            runId,
            taskId,
            reason));
    }

    /// <summary>
    /// Publishes a TaskCancelledEvent.
    /// </summary>
    public void PublishTaskCancelled(
        string workflowId,
        string runId,
        string taskId,
        string reason,
        TimeSpan duration)
    {
        _eventPublisher.PublishTaskEvent(new TaskCancelledEvent(
            workflowId,
            runId,
            taskId,
            reason,
            duration));
    }

    /// <summary>
    /// Creates a progress reporter that publishes output events.
    /// </summary>
    public IProgress<TaskProgress> CreateProgressReporter(
        string workflowId,
        string runId,
        string taskId)
    {
        return new Progress<TaskProgress>(p =>
        {
            PublishTaskOutput(workflowId, runId, taskId, p.Message, p.StreamType);
        });
    }

    /// <summary>
    /// Creates a progress reporter that publishes output events using context.
    /// </summary>
    public IProgress<TaskProgress> CreateProgressReporter(WorkflowContext context, WorkflowTask task)
    {
        return CreateProgressReporter(context.Workflow.Id, context.RunId, task.Id);
    }

    /// <summary>
    /// Publishes the appropriate completion event based on result status.
    /// </summary>
    public void PublishResultEvent(
        WorkflowContext context,
        WorkflowTask task,
        TaskResult result,
        ExecutionStats? stats = null)
    {
        switch (result.Status)
        {
            case ExecutionStatus.Skipped:
                stats?.IncrementSkipped();
                PublishTaskSkipped(
                    context.Workflow.Id,
                    context.RunId,
                    task.Id,
                    result.ErrorMessage ?? "Condition not met");
                break;

            case ExecutionStatus.Cancelled:
                stats?.IncrementFailed();
                PublishTaskCancelled(
                    context.Workflow.Id,
                    context.RunId,
                    task.Id,
                    result.ErrorMessage ?? "Task was cancelled",
                    result.Duration);
                break;

            default:
                if (result.IsSuccess)
                    stats?.IncrementSucceeded();
                else
                    stats?.IncrementFailed();

                PublishTaskCompleted(
                    context.Workflow.Id,
                    context.RunId,
                    task.Id,
                    result.Status,
                    result.ExitCode,
                    result.Duration);
                break;
        }
    }
}
