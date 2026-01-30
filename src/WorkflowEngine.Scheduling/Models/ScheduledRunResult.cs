using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Scheduling.Models;

/// <summary>
/// Represents the result of a scheduled workflow run.
/// </summary>
public sealed class ScheduledRunResult
{
    /// <summary>
    /// Gets the schedule ID that triggered this run.
    /// </summary>
    public required string ScheduleId { get; init; }

    /// <summary>
    /// Gets the unique run ID.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Gets when the run was scheduled to execute.
    /// </summary>
    public required DateTimeOffset ScheduledTime { get; init; }

    /// <summary>
    /// Gets when the run actually started.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets when the run ended, if completed.
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Gets the execution status.
    /// </summary>
    public ExecutionStatus Status { get; init; }

    /// <summary>
    /// Gets the error message if the run failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the total duration of the run.
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// Gets whether the run was triggered manually.
    /// </summary>
    public bool IsManualTrigger { get; init; }
}
