using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Strategy for building execution configuration for a specific execution environment.
/// Implementations are evaluated in order of priority (lowest value = highest priority).
/// </summary>
public interface IExecutionStrategy
{
    /// <summary>
    /// Priority of this strategy. Lower values are evaluated first.
    /// SSH = 10, Docker = 20, Local = 100 (fallback).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the name of this execution strategy for logging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether this strategy can handle the given task.
    /// </summary>
    /// <param name="workflow">The workflow definition.</param>
    /// <param name="task">The task to execute.</param>
    /// <returns>True if this strategy should handle the task.</returns>
    bool CanHandle(Workflow workflow, WorkflowTask task);

    /// <summary>
    /// Builds the execution configuration for the task.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="task">The task definition.</param>
    /// <param name="context">The workflow execution context.</param>
    /// <returns>Configuration for executing the command.</returns>
    ExecutionConfig BuildConfig(string command, WorkflowTask task, WorkflowContext context);
}
