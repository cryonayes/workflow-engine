using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Basic workflow execution capability.
/// </summary>
public interface ISimpleWorkflowRunner
{
    /// <summary>
    /// Runs a workflow and returns the final context with all results.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>The workflow context containing all task results.</returns>
    Task<WorkflowContext> RunAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Workflow execution with configurable options.
/// </summary>
public interface IConfigurableWorkflowRunner : ISimpleWorkflowRunner
{
    /// <summary>
    /// Runs a workflow with custom options.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="options">Execution options.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>The workflow context containing all task results.</returns>
    Task<WorkflowContext> RunAsync(
        Workflow workflow,
        WorkflowRunOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Workflow execution with event notifications.
/// </summary>
public interface IObservableWorkflowRunner
{
    /// <summary>
    /// Event raised for workflow lifecycle events (started, completed, cancelled).
    /// </summary>
    event EventHandler<WorkflowEvent>? OnWorkflowEvent;

    /// <summary>
    /// Event raised for task lifecycle events (started, completed, skipped, output).
    /// </summary>
    event EventHandler<TaskEvent>? OnTaskEvent;
}

/// <summary>
/// Full-featured workflow runner combining all capabilities.
/// </summary>
public interface IWorkflowRunner : IConfigurableWorkflowRunner, IObservableWorkflowRunner
{
}

/// <summary>
/// Options for workflow execution.
/// </summary>
public sealed class WorkflowRunOptions
{
    /// <summary>
    /// Gets or sets whether to perform a dry run (validate and plan without executing).
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// </summary>
    /// <value>Default is -1 (use workflow setting or unlimited).</value>
    public int MaxParallelism { get; init; } = -1;

    /// <summary>
    /// Gets or sets additional environment variables to merge with the workflow.
    /// </summary>
    public Dictionary<string, string>? AdditionalEnvironment { get; init; }

    /// <summary>
    /// Gets or sets whether to stop on first failure.
    /// </summary>
    /// <value>Default is true.</value>
    public bool StopOnFirstFailure { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to enable step mode (pause after each task for user confirmation).
    /// </summary>
    public bool StepMode { get; init; }

    /// <summary>
    /// Gets or sets the step controller for coordinating pause/resume in step mode.
    /// </summary>
    public IStepController? StepController { get; init; }

    /// <summary>
    /// Gets or sets a callback invoked when the workflow context is created.
    /// Useful for UI integration that needs access to the context during execution.
    /// </summary>
    public Action<WorkflowContext>? OnContextCreated { get; init; }

    /// <summary>
    /// Gets or sets whether to show the executing command in task output.
    /// </summary>
    /// <value>Default is true.</value>
    public bool ShowCommands { get; init; } = true;

    /// <summary>
    /// Gets or sets workflow parameters passed via CLI (--param key=value).
    /// Accessible in expressions as ${{ params.key }}.
    /// </summary>
    public Dictionary<string, string>? Parameters { get; init; }

    /// <summary>
    /// Default options instance.
    /// </summary>
    public static WorkflowRunOptions Default { get; } = new();
}

/// <summary>
/// Controls step mode execution (pause/resume synchronization).
/// </summary>
public interface IStepController
{
    /// <summary>
    /// Waits for user to signal continue. Called by the runner when paused.
    /// </summary>
    Task WaitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Signals to continue execution. Called by UI when user presses continue.
    /// </summary>
    void Release();
}
