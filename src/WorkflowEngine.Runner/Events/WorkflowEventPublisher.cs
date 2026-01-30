using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Events;

namespace WorkflowEngine.Runner.Events;

/// <summary>
/// Publishes workflow and task events with exception-safe invocation.
/// </summary>
public sealed class WorkflowEventPublisher : IEventPublisher
{
    private readonly ILogger<WorkflowEventPublisher> _logger;

    /// <inheritdoc />
    public event EventHandler<WorkflowEvent>? OnWorkflowEvent;

    /// <inheritdoc />
    public event EventHandler<TaskEvent>? OnTaskEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEventPublisher"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostics.</param>
    public WorkflowEventPublisher(ILogger<WorkflowEventPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public void PublishWorkflowEvent(WorkflowEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        try
        {
            OnWorkflowEvent?.Invoke(this, evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event handler failed for workflow event {EventType}", evt.GetType().Name);
        }
    }

    /// <inheritdoc />
    public void PublishTaskEvent(TaskEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        try
        {
            OnTaskEvent?.Invoke(this, evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event handler failed for task event {EventType}", evt.GetType().Name);
        }
    }
}
