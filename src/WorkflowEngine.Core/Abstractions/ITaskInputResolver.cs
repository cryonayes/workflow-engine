using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Resolves task input data from various sources.
/// </summary>
public interface ITaskInputResolver
{
    /// <summary>
    /// Resolves the input data for a task.
    /// </summary>
    /// <param name="task">The task with input configuration.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved input bytes, or null if no input configured.</returns>
    Task<byte[]?> ResolveInputAsync(
        WorkflowTask task,
        WorkflowContext context,
        CancellationToken cancellationToken = default);
}
