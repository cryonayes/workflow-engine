using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions;

/// <summary>
/// Resolves variable references in expressions.
/// </summary>
public interface IVariableInterpolator
{
    /// <summary>
    /// Resolves a variable expression to its value using context.Environment.
    /// </summary>
    /// <param name="expression">The expression to resolve (without ${{ }} wrapper).</param>
    /// <param name="context">The workflow context containing values.</param>
    /// <returns>The resolved value, or empty string if not found.</returns>
    string Resolve(string expression, WorkflowContext context);

    /// <summary>
    /// Resolves a variable expression to its value using a specific environment.
    /// </summary>
    /// <param name="expression">The expression to resolve (without ${{ }} wrapper).</param>
    /// <param name="context">The workflow context containing values.</param>
    /// <param name="environment">The environment dictionary to use for env.* resolution.</param>
    /// <returns>The resolved value, or empty string if not found.</returns>
    string Resolve(string expression, WorkflowContext context, IReadOnlyDictionary<string, string> environment);
}
