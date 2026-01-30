namespace WorkflowEngine.Scheduling.Models;

/// <summary>
/// Defines execution policies for scheduled workflows.
/// </summary>
public sealed class ScheduleExecutionPolicy
{
    /// <summary>
    /// Gets the maximum number of concurrent runs allowed.
    /// </summary>
    public int MaxConcurrentRuns { get; init; } = 1;

    /// <summary>
    /// Gets whether overlapping runs are allowed.
    /// </summary>
    public bool AllowOverlap { get; init; } = false;

    /// <summary>
    /// Gets the optional timeout for scheduled runs.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the maximum number of retries on failure.
    /// </summary>
    public int MaxRetries { get; init; } = 0;

    /// <summary>
    /// Gets the default execution policy.
    /// </summary>
    public static ScheduleExecutionPolicy Default { get; } = new();
}
