using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Repository for managing workflow schedules (CRUD operations).
/// </summary>
public interface IScheduleRepository
{
    /// <summary>
    /// Adds a new schedule.
    /// </summary>
    /// <param name="schedule">The schedule to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added schedule with computed NextRunAt.</returns>
    Task<WorkflowSchedule> AddScheduleAsync(
        WorkflowSchedule schedule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if removed, false if not found.</returns>
    Task<bool> RemoveScheduleAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schedule by ID.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schedule, or null if not found.</returns>
    Task<WorkflowSchedule?> GetScheduleAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all schedules.
    /// </summary>
    /// <param name="enabledOnly">If true, only return enabled schedules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of schedules.</returns>
    Task<IReadOnlyList<WorkflowSchedule>> ListSchedulesAsync(
        bool enabledOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnableScheduleAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisableScheduleAsync(string scheduleId, CancellationToken cancellationToken = default);
}
