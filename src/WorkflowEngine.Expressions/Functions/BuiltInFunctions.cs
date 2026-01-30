using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions.Functions;

/// <summary>
/// Returns true if all dependencies succeeded (or no failures with no explicit dependencies).
/// </summary>
public sealed class SuccessFunction : IExpressionFunction
{
    /// <inheritdoc />
    public string Name => "success";

    /// <inheritdoc />
    public bool Evaluate(WorkflowContext context, IReadOnlyList<string> dependsOn)
    {
        if (dependsOn.Count == 0)
            return !context.HasFailure;

        return context.DependenciesSucceeded(dependsOn);
    }
}

/// <summary>
/// Returns true if any dependency failed.
/// </summary>
public sealed class FailureFunction : IExpressionFunction
{
    /// <inheritdoc />
    public string Name => "failure";

    /// <inheritdoc />
    public bool Evaluate(WorkflowContext context, IReadOnlyList<string> dependsOn)
    {
        if (dependsOn.Count == 0)
            return context.HasFailure;

        return context.DependenciesFailed(dependsOn);
    }
}

/// <summary>
/// Always returns true, regardless of context.
/// Used for cleanup tasks that should run even on failure.
/// </summary>
public sealed class AlwaysFunction : IExpressionFunction
{
    /// <inheritdoc />
    public string Name => "always";

    /// <inheritdoc />
    public bool Evaluate(WorkflowContext context, IReadOnlyList<string> dependsOn) => true;
}

/// <summary>
/// Returns true if the workflow was cancelled.
/// </summary>
public sealed class CancelledFunction : IExpressionFunction
{
    /// <inheritdoc />
    public string Name => "cancelled";

    /// <inheritdoc />
    public bool Evaluate(WorkflowContext context, IReadOnlyList<string> dependsOn) =>
        context.OverallStatus == ExecutionStatus.Cancelled;
}
