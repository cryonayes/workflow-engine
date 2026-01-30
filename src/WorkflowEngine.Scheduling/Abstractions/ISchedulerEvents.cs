using WorkflowEngine.Scheduling.Events;

namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Provides scheduler events for monitoring scheduler activity.
/// </summary>
public interface ISchedulerEvents
{
    /// <summary>
    /// Event raised when a scheduler event occurs.
    /// </summary>
    event EventHandler<SchedulerEvent>? OnSchedulerEvent;
}
