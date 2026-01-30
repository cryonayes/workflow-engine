using System.Collections.Concurrent;

namespace WorkflowEngine.Scheduling.Execution;

/// <summary>
/// Thread-safe tracker for running scheduler jobs.
/// </summary>
public sealed class RunningJobTracker : IDisposable
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningJobs = new();
    private readonly ConcurrentDictionary<string, Task> _runningTasks = new();

    /// <summary>
    /// Gets whether a job is currently running.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    public bool IsRunning(string scheduleId) => _runningJobs.ContainsKey(scheduleId);

    /// <summary>
    /// Gets the count of running jobs.
    /// </summary>
    public int Count => _runningJobs.Count;

    /// <summary>
    /// Tries to add a new running job.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cts">The cancellation token source for the job.</param>
    /// <returns>True if added, false if already running.</returns>
    public bool TryAdd(string scheduleId, CancellationTokenSource cts)
    {
        return _runningJobs.TryAdd(scheduleId, cts);
    }

    /// <summary>
    /// Tracks a running task for a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="task">The task to track.</param>
    public void TrackTask(string scheduleId, Task task)
    {
        _runningTasks[scheduleId] = task;
    }

    /// <summary>
    /// Removes a job from tracking.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    public void Remove(string scheduleId)
    {
        _runningJobs.TryRemove(scheduleId, out _);
        _runningTasks.TryRemove(scheduleId, out _);
    }

    /// <summary>
    /// Cancels all running jobs.
    /// </summary>
    public void CancelAll()
    {
        var jobIds = _runningJobs.Keys.ToArray();
        foreach (var jobId in jobIds)
        {
            if (_runningJobs.TryGetValue(jobId, out var cts))
            {
                cts.Cancel();
            }
        }
    }

    /// <summary>
    /// Gets all currently running tasks for awaiting.
    /// </summary>
    public Task[] GetRunningTasks() => _runningTasks.Values.ToArray();

    /// <summary>
    /// Gets the cancellation token source for a job if running.
    /// </summary>
    public CancellationTokenSource? GetCancellation(string scheduleId)
    {
        return _runningJobs.TryGetValue(scheduleId, out var cts) ? cts : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancelAll();
        foreach (var cts in _runningJobs.Values)
        {
            cts.Dispose();
        }
        _runningJobs.Clear();
        _runningTasks.Clear();
    }
}
