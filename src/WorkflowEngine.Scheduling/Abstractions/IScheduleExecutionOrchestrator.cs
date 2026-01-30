using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Orchestrates the execution of scheduled workflows.
/// </summary>
public interface IScheduleExecutionOrchestrator
{
    /// <summary>
    /// Executes a schedule and returns the result.
    /// </summary>
    /// <param name="schedule">The schedule to execute.</param>
    /// <param name="isManual">Whether this is a manual trigger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<ScheduledRunResult> ExecuteAsync(
        WorkflowSchedule schedule,
        bool isManual,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a schedule with error handling and event publishing.
    /// Used for background execution where errors should be caught.
    /// </summary>
    /// <param name="schedule">The schedule to execute.</param>
    /// <param name="isManual">Whether this is a manual trigger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteWithTrackingAsync(
        WorkflowSchedule schedule,
        bool isManual,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for due schedules and executes them.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CheckAndExecuteDueSchedulesAsync(CancellationToken cancellationToken = default);
}
