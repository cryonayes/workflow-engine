using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Executes scheduled workflows.
/// </summary>
public interface IScheduleRunner
{
    /// <summary>
    /// Executes a workflow based on a schedule.
    /// </summary>
    /// <param name="schedule">The schedule that triggered this run.</param>
    /// <param name="isManual">Whether this is a manual trigger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the run.</returns>
    Task<ScheduledRunResult> RunAsync(
        WorkflowSchedule schedule,
        bool isManual = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow from a manual dispatch request.
    /// </summary>
    /// <param name="request">The dispatch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the run.</returns>
    Task<ScheduledRunResult> DispatchAsync(
        ManualDispatchRequest request,
        CancellationToken cancellationToken = default);
}
