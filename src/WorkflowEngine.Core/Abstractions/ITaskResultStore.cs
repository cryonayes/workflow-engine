namespace WorkflowEngine.Core.Abstractions;

using WorkflowEngine.Core.Models;

/// <summary>
/// Thread-safe storage for task execution results.
/// </summary>
public interface ITaskResultStore
{
    /// <summary>
    /// Records or updates the result of a task.
    /// </summary>
    /// <param name="result">The task result to store.</param>
    void Record(TaskResult result);

    /// <summary>
    /// Gets the result of a specific task by ID.
    /// </summary>
    /// <param name="taskId">The task ID to look up.</param>
    /// <returns>The task result, or null if not found.</returns>
    TaskResult? Get(string taskId);

    /// <summary>
    /// Gets all recorded task results.
    /// </summary>
    IReadOnlyDictionary<string, TaskResult> All { get; }

    /// <summary>
    /// Gets the count of recorded results.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets whether any task has failed.
    /// </summary>
    bool HasFailure { get; }

    /// <summary>
    /// Gets whether all completed tasks succeeded or were skipped.
    /// </summary>
    bool AllSucceeded { get; }

    /// <summary>
    /// Checks if all specified dependency tasks have succeeded.
    /// </summary>
    bool DependenciesSucceeded(IEnumerable<string> dependsOn);

    /// <summary>
    /// Checks if any specified dependency task has failed.
    /// </summary>
    bool DependenciesFailed(IEnumerable<string> dependsOn);
}
