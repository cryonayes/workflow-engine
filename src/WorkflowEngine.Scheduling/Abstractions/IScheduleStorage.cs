using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Provides persistence operations for workflow schedules.
/// </summary>
public interface IScheduleStorage
{
    /// <summary>
    /// Gets a schedule by ID.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schedule, or null if not found.</returns>
    Task<WorkflowSchedule?> GetAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all schedules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All schedules.</returns>
    Task<IReadOnlyList<WorkflowSchedule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled schedules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enabled schedules only.</returns>
    Task<IReadOnlyList<WorkflowSchedule>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a schedule (insert or update).
    /// </summary>
    /// <param name="schedule">The schedule to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(WorkflowSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last run time and calculates the next run time.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="lastRun">When the last run occurred.</param>
    /// <param name="nextRun">When the next run should occur.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateRunTimesAsync(
        string scheduleId,
        DateTimeOffset lastRun,
        DateTimeOffset? nextRun,
        CancellationToken cancellationToken = default);
}
