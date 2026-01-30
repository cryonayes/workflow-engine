using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions;

/// <summary>
/// String manipulation functions for expressions.
/// </summary>
public interface IStringFunctions
{
    /// <summary>
    /// Tries to evaluate a string function.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="result">The result of the evaluation.</param>
    /// <returns>True if the expression was a string function and was evaluated.</returns>
    bool TryEvaluate(string expression, WorkflowContext context, out bool result);
}
