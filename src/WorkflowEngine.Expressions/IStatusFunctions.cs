using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions;

/// <summary>
/// Provides status check functions for workflow conditions.
/// </summary>
public interface IStatusFunctions
{
    /// <summary>
    /// Returns true when all previous/dependency steps have succeeded.
    /// </summary>
    bool Success(WorkflowContext context, IEnumerable<string>? dependsOn = null);

    /// <summary>
    /// Returns true when any previous/dependency step has failed.
    /// </summary>
    bool Failure(WorkflowContext context, IEnumerable<string>? dependsOn = null);
}
