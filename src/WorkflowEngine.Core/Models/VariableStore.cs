using System.Collections.Concurrent;
using WorkflowEngine.Core.Abstractions;

namespace WorkflowEngine.Core.Models;

/// <summary>
/// Thread-safe implementation of workflow variable storage.
/// </summary>
public sealed class VariableStore : IVariableStore
{
    private readonly ConcurrentDictionary<string, object> _variables = new();

    /// <inheritdoc />
    public void Set(string name, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        _variables[name] = value;
    }

    /// <inheritdoc />
    public T? Get<T>(string name)
    {
        if (TryGet<T>(name, out var value))
            return value;
        return default;
    }

    /// <inheritdoc />
    public bool TryGet<T>(string name, out T? value)
    {
        value = default;
        if (_variables.TryGetValue(name, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }
}
