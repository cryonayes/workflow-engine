using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Scheduling.Events;

/// <summary>
/// Base class for all scheduler events.
/// </summary>
public abstract record SchedulerEvent(
    string ScheduleId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Event raised when a scheduled run is triggered.
/// </summary>
public sealed record ScheduledRunTriggeredEvent(
    string ScheduleId,
    string WorkflowPath,
    string RunId,
    bool IsManual
) : SchedulerEvent(ScheduleId, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a scheduled run completes.
/// </summary>
public sealed record ScheduledRunCompletedEvent(
    string ScheduleId,
    string RunId,
    ExecutionStatus Status,
    TimeSpan Duration,
    string? ErrorMessage = null
) : SchedulerEvent(ScheduleId, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a new schedule is added.
/// </summary>
public sealed record ScheduleAddedEvent(
    string ScheduleId,
    string WorkflowPath,
    string CronExpression,
    string? Name
) : SchedulerEvent(ScheduleId, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a schedule is removed.
/// </summary>
public sealed record ScheduleRemovedEvent(
    string ScheduleId
) : SchedulerEvent(ScheduleId, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a schedule is enabled.
/// </summary>
public sealed record ScheduleEnabledEvent(
    string ScheduleId
) : SchedulerEvent(ScheduleId, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a schedule is disabled.
/// </summary>
public sealed record ScheduleDisabledEvent(
    string ScheduleId
) : SchedulerEvent(ScheduleId, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when the scheduler starts.
/// </summary>
public sealed record SchedulerStartedEvent(
    int ActiveScheduleCount
) : SchedulerEvent(string.Empty, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when the scheduler stops.
/// </summary>
public sealed record SchedulerStoppedEvent(
    string Reason
) : SchedulerEvent(string.Empty, DateTimeOffset.UtcNow);
