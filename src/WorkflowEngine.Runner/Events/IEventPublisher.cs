using WorkflowEngine.Core.Events;

namespace WorkflowEngine.Runner.Events;

/// <summary>
/// Publishes workflow and task events with exception safety.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Event raised when a workflow-level event occurs.
    /// </summary>
    event EventHandler<WorkflowEvent>? OnWorkflowEvent;

    /// <summary>
    /// Event raised when a task-level event occurs.
    /// </summary>
    event EventHandler<TaskEvent>? OnTaskEvent;

    /// <summary>
    /// Publishes a workflow event to all subscribers.
    /// </summary>
    /// <param name="evt">The workflow event to publish.</param>
    void PublishWorkflowEvent(WorkflowEvent evt);

    /// <summary>
    /// Publishes a task event to all subscribers.
    /// </summary>
    /// <param name="evt">The task event to publish.</param>
    void PublishTaskEvent(TaskEvent evt);
}
