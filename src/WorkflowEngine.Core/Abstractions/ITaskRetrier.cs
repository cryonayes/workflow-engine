using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Provides capability to retry failed tasks.
/// </summary>
public interface ITaskRetrier
{
    /// <summary>
    /// Retries execution of a failed task.
    /// </summary>
    /// <param name="taskId">The ID of the task to retry.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task result, or null if task not found or not retryable.</returns>
    Task<TaskResult?> RetryTaskAsync(string taskId, WorkflowContext context, CancellationToken cancellationToken = default);
}
