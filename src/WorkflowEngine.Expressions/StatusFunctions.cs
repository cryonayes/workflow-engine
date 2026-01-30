using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions;

/// <summary>
/// Provides status check functions for workflow conditions.
/// </summary>
public sealed class StatusFunctions : IStatusFunctions
{
    /// <summary>
    /// Returns true when all previous/dependency steps have succeeded.
    /// </summary>
    public bool Success(WorkflowContext context, IEnumerable<string>? dependsOn = null) =>
        dependsOn?.Any() == true
            ? context.DependenciesSucceeded(dependsOn)
            : context.AllSucceeded;

    /// <summary>
    /// Returns true when any previous/dependency step has failed.
    /// </summary>
    public bool Failure(WorkflowContext context, IEnumerable<string>? dependsOn = null) =>
        dependsOn?.Any() == true
            ? context.DependenciesFailed(dependsOn)
            : context.HasFailure;
}
