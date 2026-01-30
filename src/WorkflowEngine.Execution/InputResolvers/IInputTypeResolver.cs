using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.InputResolvers;

/// <summary>
/// Strategy interface for resolving task input of a specific type.
/// </summary>
public interface IInputTypeResolver
{
    /// <summary>
    /// Gets the input type this resolver handles.
    /// </summary>
    InputType SupportedType { get; }

    /// <summary>
    /// Resolves the input value to bytes.
    /// </summary>
    /// <param name="input">The task input configuration.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved input as bytes, or null if no input.</returns>
    Task<byte[]?> ResolveAsync(TaskInput input, WorkflowContext context, CancellationToken cancellationToken);
}
