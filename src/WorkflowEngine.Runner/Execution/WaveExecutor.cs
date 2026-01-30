using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;
using WorkflowEngine.Runner.StepMode;

namespace WorkflowEngine.Runner.Execution;

/// <summary>
/// Executes waves of tasks with parallel or step-mode execution.
/// </summary>
public sealed class WaveExecutor : IWaveExecutor
{
    private readonly ITaskExecutor _taskExecutor;
    private readonly TaskEventHelper _taskEventHelper;
    private readonly IStepModeHandler _stepModeHandler;
    private readonly ILogger<WaveExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveExecutor"/> class.
    /// </summary>
    public WaveExecutor(
        ITaskExecutor taskExecutor,
        IEventPublisher eventPublisher,
        IStepModeHandler stepModeHandler,
        ILogger<WaveExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(taskExecutor);
        ArgumentNullException.ThrowIfNull(eventPublisher);
        ArgumentNullException.ThrowIfNull(stepModeHandler);
        ArgumentNullException.ThrowIfNull(logger);

        _taskExecutor = taskExecutor;
        _taskEventHelper = new TaskEventHelper(eventPublisher);
        _stepModeHandler = stepModeHandler;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<TaskResult>> ExecuteWaveAsync(WaveExecutionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        return ctx.IsStepMode
            ? ExecuteWaveStepModeInternal(ctx)
            : ExecuteWaveParallelInternal(ctx);
    }

    private async Task<List<TaskResult>> ExecuteWaveParallelInternal(WaveExecutionContext ctx)
    {
        var waveTasks = ctx.Wave.Tasks.Select(task =>
        {
            var currentIndex = ctx.Stats.IncrementTaskIndex();
            return ExecuteTaskAsyncCore(task, ctx.Context, ctx.Stats, currentIndex, ctx.TotalTasks, ctx.CancellationToken);
        });

        var results = await Task.WhenAll(waveTasks);
        return results.ToList();
    }

    private async Task<List<TaskResult>> ExecuteWaveStepModeInternal(WaveExecutionContext ctx)
    {
        var results = new List<TaskResult>();

        for (var i = 0; i < ctx.Wave.Tasks.Count; i++)
        {
            var task = ctx.Wave.Tasks[i];
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var currentIndex = ctx.Stats.IncrementTaskIndex();
            var result = await ExecuteTaskAsyncCore(task, ctx.Context, ctx.Stats, currentIndex, ctx.TotalTasks, ctx.CancellationToken);
            results.Add(result);

            // Pause after each task except the last one in the wave
            var isLastTaskInWave = i == ctx.Wave.Tasks.Count - 1;
            if (ctx.Options.StepController != null && !isLastTaskInWave)
            {
                await _stepModeHandler.PauseAsync(ctx.Context, task.Id, ctx.Stats.TotalCompleted, ctx.TotalTasks, ctx.Options, ctx.CancellationToken);
            }
        }

        return results;
    }

    private async Task<TaskResult> ExecuteTaskAsyncCore(
        WorkflowTask task,
        WorkflowContext context,
        ExecutionStats stats,
        int taskIndex,
        int totalTasks,
        CancellationToken cancellationToken)
    {
        _taskEventHelper.PublishTaskStarted(context, task, taskIndex, totalTasks);

        var progress = _taskEventHelper.CreateProgressReporter(context, task);

        // Create linked token for task-specific cancellation support
        var taskCts = context.GetOrCreateTaskCancellation(task.Id);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, taskCts.Token);

        TaskResult result;
        try
        {
            result = await _taskExecutor.ExecuteAsync(task, context, progress, linkedCts.Token);
        }
        finally
        {
            context.RemoveTaskCancellation(task.Id);
        }

        context.RecordTaskResult(result);
        _taskEventHelper.PublishResultEvent(context, task, result, stats);

        return result;
    }
}
