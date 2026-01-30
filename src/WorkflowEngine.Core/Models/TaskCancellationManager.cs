using System.Collections.Concurrent;
using WorkflowEngine.Core.Abstractions;

namespace WorkflowEngine.Core.Models;

/// <summary>
/// Thread-safe manager for task cancellation tokens.
/// </summary>
public sealed class TaskCancellationManager : ITaskCancellationManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _taskCancellations = new();

    /// <inheritdoc />
    public CancellationTokenSource GetOrCreate(string taskId)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        return _taskCancellations.GetOrAdd(taskId, _ => new CancellationTokenSource());
    }

    /// <inheritdoc />
    public void RequestCancellation(string taskId)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        if (_taskCancellations.TryGetValue(taskId, out var cts))
        {
            cts.Cancel();
        }
    }

    /// <inheritdoc />
    public void Remove(string taskId)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        if (_taskCancellations.TryRemove(taskId, out var cts))
        {
            cts.Dispose();
        }
    }
}
