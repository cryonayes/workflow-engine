using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions.Functions;

/// <summary>
/// Interface for expression functions that can be evaluated in conditions.
/// </summary>
public interface IExpressionFunction
{
    /// <summary>
    /// Gets the name of the function (e.g., "success", "failure", "always").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the function.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="dependsOn">Task dependencies for context-aware evaluation.</param>
    /// <returns>The function result.</returns>
    bool Evaluate(WorkflowContext context, IReadOnlyList<string> dependsOn);
}
