using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Evaluates condition expressions to boolean values.
/// </summary>
public interface IConditionEvaluator
{
    /// <summary>
    /// Evaluates a condition expression to a boolean value.
    /// </summary>
    /// <param name="expression">The expression to evaluate (e.g., "${{ success() }}").</param>
    /// <param name="context">The workflow context containing task results and variables.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    /// <exception cref="ExpressionEvaluationException">Thrown if the expression cannot be evaluated.</exception>
    bool EvaluateCondition(string expression, WorkflowContext context);

    /// <summary>
    /// Evaluates a condition expression with specific task dependencies.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="dependsOn">The task IDs to consider for status functions.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    bool EvaluateCondition(string expression, WorkflowContext context, IEnumerable<string>? dependsOn);
}

/// <summary>
/// Interpolates variable references in string templates.
/// </summary>
public interface IExpressionInterpolator
{
    /// <summary>
    /// Interpolates variable references in a string template.
    /// Uses the full environment from context (including host system variables for local execution).
    /// </summary>
    /// <param name="template">The template string containing expressions.</param>
    /// <param name="context">The workflow context containing values to substitute.</param>
    /// <returns>The template with all expressions replaced with their values.</returns>
    /// <example>
    /// <code>
    /// var result = interpolator.Interpolate(
    ///     "Hello ${{ env.NAME }}, previous output was: ${{ tasks.step1.output }}",
    ///     context
    /// );
    /// </code>
    /// </example>
    string Interpolate(string template, WorkflowContext context);

    /// <summary>
    /// Interpolates variable references in a string template using a specific environment.
    /// Use this overload for containerized execution to prevent host environment leakage.
    /// </summary>
    /// <param name="template">The template string containing expressions.</param>
    /// <param name="context">The workflow context containing values to substitute.</param>
    /// <param name="environment">The environment dictionary to use for ${{ env.* }} resolution.</param>
    /// <returns>The template with all expressions replaced with their values.</returns>
    string Interpolate(string template, WorkflowContext context, IReadOnlyDictionary<string, string> environment);
}

/// <summary>
/// Combined interface for expression evaluation and interpolation.
/// </summary>
public interface IExpressionEvaluator : IConditionEvaluator, IExpressionInterpolator
{
}
