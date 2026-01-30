using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;
using WorkflowEngine.Runner.Execution;
using WorkflowEngine.Runner.StepMode;

namespace WorkflowEngine.Runner;

/// <summary>
/// Orchestrates workflow execution with parallel task scheduling and event publishing.
/// </summary>
public sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly IExecutionScheduler _scheduler;
    private readonly IWaveExecutor _waveExecutor;
    private readonly IStepModeHandler _stepModeHandler;
    private readonly IEventPublisher _eventPublisher;
    private readonly IWebhookNotifier? _webhookNotifier;
    private readonly ILogger<WorkflowRunner> _logger;

    /// <inheritdoc />
    public event EventHandler<WorkflowEvent>? OnWorkflowEvent
    {
        add => _eventPublisher.OnWorkflowEvent += value;
        remove => _eventPublisher.OnWorkflowEvent -= value;
    }

    /// <inheritdoc />
    public event EventHandler<TaskEvent>? OnTaskEvent
    {
        add => _eventPublisher.OnTaskEvent += value;
        remove => _eventPublisher.OnTaskEvent -= value;
    }

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    public WorkflowRunner(
        IExecutionScheduler scheduler,
        IWaveExecutor waveExecutor,
        IStepModeHandler stepModeHandler,
        IEventPublisher eventPublisher,
        ILogger<WorkflowRunner> logger,
        IWebhookNotifier? webhookNotifier = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(waveExecutor);
        ArgumentNullException.ThrowIfNull(stepModeHandler);
        ArgumentNullException.ThrowIfNull(eventPublisher);
        ArgumentNullException.ThrowIfNull(logger);

        _scheduler = scheduler;
        _waveExecutor = waveExecutor;
        _stepModeHandler = stepModeHandler;
        _eventPublisher = eventPublisher;
        _webhookNotifier = webhookNotifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<WorkflowContext> RunAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        return RunAsync(workflow, WorkflowRunOptions.Default, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowContext> RunAsync(
        Workflow workflow,
        WorkflowRunOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(options);

        var context = WorkflowContextFactory.Create(workflow, options, cancellationToken);
        options.OnContextCreated?.Invoke(context);

        var plan = _scheduler.BuildExecutionPlan(workflow);

        var modeDescription = options.StepMode ? "step mode" : "normal mode";
        _logger.LogInformation(
            "Starting workflow '{WorkflowName}' (run: {RunId}) with {TaskCount} tasks in {WaveCount} waves ({Mode})",
            workflow.Name, context.RunId, plan.TotalTasks, plan.WaveCount, modeDescription);

        // Register webhooks if any are configured
        if (workflow.Webhooks.Count > 0 && _webhookNotifier is not null)
        {
            _webhookNotifier.RegisterWebhooks(context.RunId, workflow.Name, workflow.Webhooks);
            _logger.LogDebug("Registered {Count} webhooks for workflow '{Name}'",
                workflow.Webhooks.Count, workflow.Name);
        }

        _eventPublisher.PublishWorkflowEvent(new WorkflowStartedEvent(
            workflow.Id,
            context.RunId,
            workflow.Name,
            plan.TotalTasks));

        var startTime = DateTimeOffset.UtcNow;
        var stats = new ExecutionStats();

        try
        {
            if (options.DryRun)
            {
                _logger.LogInformation("Dry run mode - skipping actual execution");
                return context;
            }

            // In step mode, pause before the first task
            if (_stepModeHandler.ShouldPause(options) && plan.TotalTasks > 0)
            {
                await _stepModeHandler.PauseAsync(context, "", 0, plan.TotalTasks, options, cancellationToken);
            }

            // Execute regular waves
            foreach (var wave in plan.Waves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Executing wave {WaveIndex} with {TaskCount} tasks",
                    wave.WaveIndex, wave.Count);

                var waveResults = await ExecuteWaveWithEventsAsync(
                    wave, context, stats, plan, options, cancellationToken);

                // Check for critical failures
                if (options.StopOnFirstFailure && HasCriticalFailure(wave, waveResults))
                {
                    _logger.LogWarning("Stopping workflow due to critical failure in wave {WaveIndex}",
                        wave.WaveIndex);
                    break;
                }

                // In step mode, pause after wave completes (before next wave starts)
                var isLastWave = wave.WaveIndex == plan.WaveCount - 1 && plan.AlwaysTasks.Count == 0;
                if (_stepModeHandler.ShouldPause(options) && !isLastWave)
                {
                    var lastTask = wave.Tasks[^1];
                    await _stepModeHandler.PauseAsync(context, lastTask.Id, stats.TotalCompleted, plan.TotalTasks, options, cancellationToken);
                }
            }

            // Execute always() tasks
            if (plan.AlwaysTasks.Count > 0)
            {
                await ExecuteAlwaysTasksAsync(plan, context, stats, options);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Workflow '{WorkflowName}' was cancelled", workflow.Name);
            context.MarkCancelled();

            _eventPublisher.PublishWorkflowEvent(new WorkflowCancelledEvent(
                workflow.Id,
                context.RunId,
                "Workflow was cancelled by user request"));

            throw;
        }
        finally
        {
            var duration = DateTimeOffset.UtcNow - startTime;

            _logger.LogInformation(
                "Workflow '{WorkflowName}' completed with status {Status} " +
                "(succeeded: {Succeeded}, failed: {Failed}, skipped: {Skipped}, duration: {Duration:F2}s)",
                workflow.Name, context.OverallStatus,
                stats.Succeeded, stats.Failed, stats.Skipped, duration.TotalSeconds);

            _eventPublisher.PublishWorkflowEvent(new WorkflowCompletedEvent(
                workflow.Id,
                context.RunId,
                context.OverallStatus,
                duration,
                stats.Succeeded,
                stats.Failed,
                stats.Skipped));

            // Unregister webhooks after completion event is published
            _webhookNotifier?.UnregisterWebhooks(context.RunId);
        }

        return context;
    }

    private async Task ExecuteAlwaysTasksAsync(
        ExecutionPlan plan,
        WorkflowContext context,
        ExecutionStats stats,
        WorkflowRunOptions options)
    {
        _logger.LogDebug("Executing {Count} always() tasks", plan.AlwaysTasks.Count);

        // Create a synthetic wave for always tasks
        var alwaysWave = new ExecutionWave { WaveIndex = plan.WaveCount, Tasks = plan.AlwaysTasks.ToList() };

        // Always tasks run even if cancelled, so use CancellationToken.None
        await ExecuteWaveWithEventsAsync(alwaysWave, context, stats, plan, options, CancellationToken.None);
    }

    private async Task<List<TaskResult>> ExecuteWaveWithEventsAsync(
        ExecutionWave wave,
        WorkflowContext context,
        ExecutionStats stats,
        ExecutionPlan plan,
        WorkflowRunOptions options,
        CancellationToken cancellationToken)
    {
        _eventPublisher.PublishWorkflowEvent(new WaveStartedEvent(
            context.Workflow.Id,
            context.RunId,
            wave.WaveIndex,
            plan.WaveCount + (plan.AlwaysTasks.Count > 0 ? 1 : 0),
            wave.Tasks.Select(t => t.Id).ToList()));

        // Create execution context for the wave
        var waveContext = new WaveExecutionContext(
            wave, context, stats, plan.TotalTasks, options, cancellationToken);

        var waveResults = await _waveExecutor.ExecuteWaveAsync(waveContext);

        _eventPublisher.PublishWorkflowEvent(new WaveCompletedEvent(
            context.Workflow.Id,
            context.RunId,
            wave.WaveIndex,
            waveResults.Count(r => r.IsSuccess),
            waveResults.Count(r => r.IsFailed),
            waveResults.Count(r => r.WasSkipped)));

        return waveResults;
    }

    private static bool HasCriticalFailure(ExecutionWave wave, List<TaskResult> results) =>
        results.Any(r =>
            r.IsFailed &&
            wave.Tasks.FirstOrDefault(t => t.Id == r.TaskId) is { ContinueOnError: false });
}
