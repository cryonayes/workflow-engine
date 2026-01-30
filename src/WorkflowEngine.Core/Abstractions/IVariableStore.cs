namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Thread-safe storage for workflow variables.
/// </summary>
public interface IVariableStore
{
    /// <summary>
    /// Sets a workflow variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    void Set(string name, object value);

    /// <summary>
    /// Gets a workflow variable.
    /// </summary>
    /// <typeparam name="T">The expected type of the variable.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>The variable value, or default if not found or wrong type.</returns>
    T? Get<T>(string name);

    /// <summary>
    /// Tries to get a workflow variable.
    /// </summary>
    /// <typeparam name="T">The expected type of the variable.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value if found.</param>
    /// <returns>True if the variable was found and is the correct type.</returns>
    bool TryGet<T>(string name, out T? value);
}
