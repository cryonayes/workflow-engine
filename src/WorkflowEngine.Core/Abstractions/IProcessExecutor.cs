using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Executes shell processes with real-time output streaming.
/// </summary>
public interface IProcessExecutor
{
    /// <summary>
    /// Executes a shell command and returns the result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="task">The task definition containing execution settings.</param>
    /// <param name="context">The workflow context with environment and state.</param>
    /// <param name="input">Optional stdin input bytes.</param>
    /// <param name="progress">Optional progress reporter for live output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<TaskResult> ExecuteAsync(
        string command,
        WorkflowTask task,
        WorkflowContext context,
        byte[]? input,
        IProgress<TaskProgress>? progress,
        CancellationToken cancellationToken);
}
