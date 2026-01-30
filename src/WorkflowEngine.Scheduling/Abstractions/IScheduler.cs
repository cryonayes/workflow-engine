namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Main scheduler service for managing and executing scheduled workflows.
/// Combines lifecycle, repository, execution, and event capabilities.
/// </summary>
/// <remarks>
/// This interface combines multiple focused interfaces for convenience.
/// For more targeted dependencies, use the individual interfaces:
/// <list type="bullet">
///   <item><see cref="ISchedulerLifecycle"/> - Start, stop, and check running status</item>
///   <item><see cref="IScheduleRepository"/> - CRUD operations on schedules</item>
///   <item><see cref="IScheduleExecutor"/> - Trigger and dispatch workflows</item>
///   <item><see cref="ISchedulerEvents"/> - Subscribe to scheduler events</item>
/// </list>
/// </remarks>
public interface IScheduler : ISchedulerLifecycle, IScheduleRepository, IScheduleExecutor, ISchedulerEvents
{
}
