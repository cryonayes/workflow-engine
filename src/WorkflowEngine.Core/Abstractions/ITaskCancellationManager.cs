namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Manages cancellation tokens for individual tasks.
/// </summary>
public interface ITaskCancellationManager
{
    /// <summary>
    /// Gets or creates a cancellation token source for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <returns>The cancellation token source for the task.</returns>
    CancellationTokenSource GetOrCreate(string taskId);

    /// <summary>
    /// Requests cancellation of a specific task.
    /// </summary>
    /// <param name="taskId">The task ID to cancel.</param>
    void RequestCancellation(string taskId);

    /// <summary>
    /// Removes the cancellation token source for a task (cleanup after execution).
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    void Remove(string taskId);
}
