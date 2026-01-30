namespace WorkflowEngine.Scheduling.Models;

/// <summary>
/// Represents a scheduled workflow execution configuration.
/// </summary>
public sealed class WorkflowSchedule
{
    /// <summary>
    /// Gets the unique identifier for this schedule.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the path to the workflow YAML file.
    /// </summary>
    public required string WorkflowPath { get; init; }

    /// <summary>
    /// Gets the cron expression defining when to run (e.g., "0 2 * * *" for 2 AM daily).
    /// </summary>
    public required string CronExpression { get; init; }

    /// <summary>
    /// Gets the optional display name for this schedule.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this schedule is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets when this schedule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when the workflow was last executed by this schedule.
    /// </summary>
    public DateTimeOffset? LastRunAt { get; init; }

    /// <summary>
    /// Gets the next scheduled execution time.
    /// </summary>
    public DateTimeOffset? NextRunAt { get; init; }

    /// <summary>
    /// Gets the input parameters to pass to the workflow as environment variables.
    /// </summary>
    public IReadOnlyDictionary<string, string> InputParameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the execution policy for this schedule.
    /// </summary>
    public ScheduleExecutionPolicy ExecutionPolicy { get; init; } = ScheduleExecutionPolicy.Default;

    /// <summary>
    /// Creates a copy of this schedule with updated properties.
    /// </summary>
    public WorkflowSchedule With(
        DateTimeOffset? lastRunAt = null,
        DateTimeOffset? nextRunAt = null,
        bool? enabled = null)
    {
        return new WorkflowSchedule
        {
            Id = Id,
            WorkflowPath = WorkflowPath,
            CronExpression = CronExpression,
            Name = Name,
            Description = Description,
            Enabled = enabled ?? Enabled,
            CreatedAt = CreatedAt,
            LastRunAt = lastRunAt ?? LastRunAt,
            NextRunAt = nextRunAt ?? NextRunAt,
            InputParameters = InputParameters,
            ExecutionPolicy = ExecutionPolicy
        };
    }
}
