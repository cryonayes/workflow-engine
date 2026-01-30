using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="ExecutionStatus"/>.
/// </summary>
public static class ExecutionStatusExtensions
{
    /// <summary>
    /// Determines whether the status represents a successful execution.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <returns>True if the status is Succeeded and exit code is 0.</returns>
    public static bool IsSuccessful(this ExecutionStatus status, int exitCode)
        => status == ExecutionStatus.Succeeded && exitCode == 0;
}
