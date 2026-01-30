using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;

namespace WorkflowEngine.Runner.StepMode;

/// <summary>
/// Handles step mode pausing and resuming during workflow execution.
/// </summary>
public sealed class StepModeHandler : IStepModeHandler
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<StepModeHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepModeHandler"/> class.
    /// </summary>
    public StepModeHandler(IEventPublisher eventPublisher, ILogger<StepModeHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(eventPublisher);
        ArgumentNullException.ThrowIfNull(logger);

        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PauseAsync(
        WorkflowContext context,
        string completedTaskId,
        int completedTasks,
        int totalTasks,
        WorkflowRunOptions options,
        CancellationToken cancellationToken)
    {
        if (options.StepController == null) return;

        var isStart = string.IsNullOrEmpty(completedTaskId);
        _logger.LogDebug(isStart
            ? "Step mode: waiting for user to start"
            : "Step mode: paused after task '{TaskId}'", completedTaskId);

        _eventPublisher.PublishWorkflowEvent(new StepPausedEvent(
            context.Workflow.Id,
            context.RunId,
            completedTaskId,
            completedTasks,
            totalTasks));

        await options.StepController.WaitAsync(cancellationToken);

        _eventPublisher.PublishWorkflowEvent(new StepResumedEvent(
            context.Workflow.Id,
            context.RunId));

        _logger.LogDebug("Step mode: continuing");
    }

    /// <inheritdoc />
    public bool ShouldPause(WorkflowRunOptions options) =>
        options.StepMode && options.StepController != null;
}
