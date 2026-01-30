using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;

namespace WorkflowEngine.Runner;

/// <summary>
/// Provides capability to retry failed tasks during workflow execution.
/// </summary>
public sealed class TaskRetrier : ITaskRetrier
{
    private readonly ITaskExecutor _taskExecutor;
    private readonly TaskEventHelper _taskEventHelper;
    private readonly ILogger<TaskRetrier> _logger;

    /// <summary>
    /// Initializes a new instance of the TaskRetrier class.
    /// </summary>
    public TaskRetrier(
        ITaskExecutor taskExecutor,
        IEventPublisher eventPublisher,
        ILogger<TaskRetrier> logger)
    {
        _taskExecutor = taskExecutor ?? throw new ArgumentNullException(nameof(taskExecutor));
        ArgumentNullException.ThrowIfNull(eventPublisher);
        _taskEventHelper = new TaskEventHelper(eventPublisher);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TaskResult?> RetryTaskAsync(
        string taskId,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskId);
        ArgumentNullException.ThrowIfNull(context);

        var workflow = context.Workflow;

        // Find the task in the workflow
        var task = workflow.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            _logger.LogWarning("Cannot retry task '{TaskId}': task not found in workflow", taskId);
            return null;
        }

        // Check if task is in a retryable state (failed or timed out)
        var previousResult = context.GetTaskResult(taskId);
        if (previousResult is null ||
            (previousResult.Status != ExecutionStatus.Failed && previousResult.Status != ExecutionStatus.TimedOut))
        {
            _logger.LogWarning("Cannot retry task '{TaskId}': task has not failed (status: {Status})",
                taskId, previousResult?.Status.ToString() ?? "not executed");
            return null;
        }

        _logger.LogInformation("Retrying task '{TaskId}'", taskId);

        var taskIndex = workflow.Tasks.ToList().IndexOf(task);

        // Create progress reporter that publishes events
        var progress = _taskEventHelper.CreateProgressReporter(context, task);

        // Publish task started event
        _taskEventHelper.PublishTaskStarted(context, task, taskIndex, workflow.Tasks.Count);

        try
        {
            // Execute the task
            var result = await _taskExecutor.ExecuteAsync(task, context, progress, cancellationToken);

            // Record the new result (overwrites the old one)
            context.RecordTaskResult(result);

            // Publish completion event
            _taskEventHelper.PublishTaskCompleted(
                workflow.Id,
                context.RunId,
                taskId,
                result.Status,
                result.ExitCode,
                result.Duration);

            _logger.LogInformation("Task '{TaskId}' retry completed with status {Status}", taskId, result.Status);

            return result;
        }
        catch (OperationCanceledException)
        {
            var cancelResult = TaskResult.Cancelled(taskId, DateTimeOffset.UtcNow);
            context.RecordTaskResult(cancelResult);

            _taskEventHelper.PublishTaskCancelled(
                workflow.Id,
                context.RunId,
                taskId,
                "User cancelled",
                cancelResult.Duration);

            _logger.LogWarning("Task '{TaskId}' retry was cancelled", taskId);
            return cancelResult;
        }
    }
}
