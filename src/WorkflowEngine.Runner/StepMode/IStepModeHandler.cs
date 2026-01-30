using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.StepMode;

/// <summary>
/// Handles step mode pausing and resuming during workflow execution.
/// </summary>
public interface IStepModeHandler
{
    /// <summary>
    /// Pauses execution in step mode after a task completes.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="completedTaskId">The ID of the completed task (empty for initial pause).</param>
    /// <param name="completedTasks">Number of tasks completed so far.</param>
    /// <param name="totalTasks">Total number of tasks in the workflow.</param>
    /// <param name="options">The workflow run options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PauseAsync(
        WorkflowContext context,
        string completedTaskId,
        int completedTasks,
        int totalTasks,
        WorkflowRunOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Determines if step mode pause should occur.
    /// </summary>
    /// <param name="options">The workflow run options.</param>
    /// <returns>True if step mode is enabled and should pause.</returns>
    bool ShouldPause(WorkflowRunOptions options);
}
