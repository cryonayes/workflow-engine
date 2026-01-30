namespace WorkflowEngine.Expressions.Functions;

/// <summary>
/// Default implementation of function registry.
/// </summary>
public sealed class FunctionRegistry : IFunctionRegistry
{
    private readonly Dictionary<string, IExpressionFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyCollection<string> FunctionNames => _functions.Keys;

    /// <summary>
    /// Creates a registry with default functions.
    /// </summary>
    public static FunctionRegistry CreateDefault()
    {
        var registry = new FunctionRegistry();
        registry.Register(new SuccessFunction());
        registry.Register(new FailureFunction());
        registry.Register(new AlwaysFunction());
        registry.Register(new CancelledFunction());
        return registry;
    }

    /// <inheritdoc />
    public void Register(IExpressionFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);
        _functions[function.Name] = function;
    }

    /// <inheritdoc />
    public IExpressionFunction? Get(string name)
    {
        return _functions.TryGetValue(name, out var function) ? function : null;
    }

    /// <inheritdoc />
    public bool Contains(string name) => _functions.ContainsKey(name);
}
