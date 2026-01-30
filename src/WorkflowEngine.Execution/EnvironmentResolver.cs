using System.Collections;
using WorkflowEngine.Core.Abstractions;

namespace WorkflowEngine.Execution;

/// <summary>
/// Default implementation of environment variable resolution.
/// </summary>
public sealed class EnvironmentResolver : IEnvironmentResolver
{
    private IReadOnlyDictionary<string, string>? _cachedSystemEnv;
    private readonly object _cacheLock = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ResolveForLocalExecution(
        IReadOnlyDictionary<string, string>? workflowEnv = null,
        IReadOnlyDictionary<string, string>? taskEnv = null,
        IReadOnlyDictionary<string, string>? additionalEnv = null)
    {
        // Start with system environment
        var result = new Dictionary<string, string>(GetSystemEnvironment());

        // Layer declared variables (workflow -> additional -> task)
        MergeInto(result, workflowEnv);
        MergeInto(result, additionalEnv);
        MergeInto(result, taskEnv);

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ResolveForDockerExecution(
        IReadOnlyDictionary<string, string>? workflowEnv = null,
        IReadOnlyDictionary<string, string>? taskEnv = null,
        IReadOnlyDictionary<string, string>? additionalEnv = null)
    {
        // For Docker, don't include system environment (prevent host leakage)
        var result = new Dictionary<string, string>();

        // Layer declared variables only (workflow -> additional -> task)
        MergeInto(result, workflowEnv);
        MergeInto(result, additionalEnv);
        MergeInto(result, taskEnv);

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetSystemEnvironment()
    {
        if (_cachedSystemEnv is not null)
            return _cachedSystemEnv;

        lock (_cacheLock)
        {
            if (_cachedSystemEnv is not null)
                return _cachedSystemEnv;

            var systemEnv = new Dictionary<string, string>();
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    systemEnv[key] = value;
                }
            }
            _cachedSystemEnv = systemEnv;
            return _cachedSystemEnv;
        }
    }

    private static void MergeInto(
        Dictionary<string, string> target,
        IReadOnlyDictionary<string, string>? source)
    {
        if (source is null) return;

        foreach (var (key, value) in source)
        {
            target[key] = value;
        }
    }
}
