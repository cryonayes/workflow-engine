namespace WorkflowEngine.Expressions.Functions;

/// <summary>
/// Registry for expression functions.
/// Provides OCP-compliant function registration and lookup.
/// </summary>
public interface IFunctionRegistry
{
    /// <summary>
    /// Gets all registered function names.
    /// </summary>
    IReadOnlyCollection<string> FunctionNames { get; }

    /// <summary>
    /// Registers a function.
    /// </summary>
    /// <param name="function">The function to register.</param>
    void Register(IExpressionFunction function);

    /// <summary>
    /// Gets a function by name.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <returns>The function if found, null otherwise.</returns>
    IExpressionFunction? Get(string name);

    /// <summary>
    /// Checks if a function is registered.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <returns>True if registered.</returns>
    bool Contains(string name);
}
