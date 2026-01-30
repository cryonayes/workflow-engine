using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Execution;

/// <summary>
/// Context containing all parameters needed for wave execution.
/// Consolidates the data clump of (wave, context, stats, totalTasks, options, token).
/// </summary>
/// <param name="Wave">The wave of tasks to execute.</param>
/// <param name="Context">The workflow execution context.</param>
/// <param name="Stats">The execution statistics tracker.</param>
/// <param name="TotalTasks">Total number of tasks in the workflow.</param>
/// <param name="Options">The workflow run options.</param>
/// <param name="CancellationToken">Token to cancel execution.</param>
public sealed record WaveExecutionContext(
    ExecutionWave Wave,
    WorkflowContext Context,
    ExecutionStats Stats,
    int TotalTasks,
    WorkflowRunOptions Options,
    CancellationToken CancellationToken)
{
    /// <summary>
    /// Creates a context for parallel execution (no step mode options needed).
    /// </summary>
    public static WaveExecutionContext ForParallel(
        ExecutionWave wave,
        WorkflowContext context,
        ExecutionStats stats,
        int totalTasks,
        CancellationToken cancellationToken)
    {
        return new WaveExecutionContext(
            wave, context, stats, totalTasks, WorkflowRunOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Creates a context for step mode execution.
    /// </summary>
    public static WaveExecutionContext ForStepMode(
        ExecutionWave wave,
        WorkflowContext context,
        ExecutionStats stats,
        int totalTasks,
        WorkflowRunOptions options,
        CancellationToken cancellationToken)
    {
        return new WaveExecutionContext(wave, context, stats, totalTasks, options, cancellationToken);
    }

    /// <summary>
    /// Gets whether step mode is enabled.
    /// </summary>
    public bool IsStepMode => Options.StepMode && Options.StepController is not null;
}
