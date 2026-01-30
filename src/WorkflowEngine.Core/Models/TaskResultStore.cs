using System.Collections.Concurrent;
using WorkflowEngine.Core.Abstractions;

namespace WorkflowEngine.Core.Models;

/// <summary>
/// Thread-safe implementation of task result storage.
/// </summary>
public sealed class TaskResultStore : ITaskResultStore
{
    private readonly ConcurrentDictionary<string, TaskResult> _taskResults = new();

    /// <inheritdoc />
    public void Record(TaskResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _taskResults[result.TaskId] = result;
    }

    /// <inheritdoc />
    public TaskResult? Get(string taskId) =>
        _taskResults.TryGetValue(taskId, out var result) ? result : null;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, TaskResult> All => _taskResults;

    /// <inheritdoc />
    public int Count => _taskResults.Count;

    /// <inheritdoc />
    public bool HasFailure => _taskResults.Values.Any(r => r.IsFailed);

    /// <inheritdoc />
    public bool AllSucceeded => _taskResults.Values.All(r => r.IsSuccess || r.WasSkipped);

    /// <inheritdoc />
    public bool DependenciesSucceeded(IEnumerable<string> dependsOn) =>
        dependsOn.All(dep => Get(dep)?.IsSuccess == true);

    /// <inheritdoc />
    public bool DependenciesFailed(IEnumerable<string> dependsOn) =>
        dependsOn.Any(dep => Get(dep)?.IsFailed == true);
}
