namespace WorkflowEngine.Execution;

/// <summary>
/// Utility for merging environment variable dictionaries.
/// Override values take precedence over base values.
/// </summary>
public static class EnvironmentMerger
{
    /// <summary>
    /// Merges two environment variable dictionaries.
    /// Values from the override dictionary take precedence.
    /// </summary>
    /// <param name="baseEnvironment">Base environment variables.</param>
    /// <param name="overrideEnvironment">Override environment variables (takes precedence).</param>
    /// <returns>Merged dictionary with all environment variables.</returns>
    public static Dictionary<string, string> Merge(
        IReadOnlyDictionary<string, string> baseEnvironment,
        IReadOnlyDictionary<string, string> overrideEnvironment)
    {
        ArgumentNullException.ThrowIfNull(baseEnvironment);
        ArgumentNullException.ThrowIfNull(overrideEnvironment);

        var result = new Dictionary<string, string>(baseEnvironment);
        foreach (var (key, value) in overrideEnvironment)
        {
            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// Merges multiple environment variable dictionaries.
    /// Later dictionaries take precedence over earlier ones.
    /// </summary>
    /// <param name="environments">Environment dictionaries to merge, in order of increasing precedence.</param>
    /// <returns>Merged dictionary with all environment variables.</returns>
    public static Dictionary<string, string> Merge(params IReadOnlyDictionary<string, string>[] environments)
    {
        if (environments.Length == 0)
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(environments[0]);
        for (var i = 1; i < environments.Length; i++)
        {
            foreach (var (key, value) in environments[i])
            {
                result[key] = value;
            }
        }
        return result;
    }

    /// <summary>
    /// Creates a merged read-only dictionary without copying if possible.
    /// </summary>
    /// <param name="baseEnvironment">Base environment variables.</param>
    /// <param name="overrideEnvironment">Override environment variables (takes precedence).</param>
    /// <returns>Read-only dictionary with merged environment variables.</returns>
    public static IReadOnlyDictionary<string, string> MergeReadOnly(
        IReadOnlyDictionary<string, string> baseEnvironment,
        IReadOnlyDictionary<string, string> overrideEnvironment)
    {
        ArgumentNullException.ThrowIfNull(baseEnvironment);
        ArgumentNullException.ThrowIfNull(overrideEnvironment);

        // Optimize for common cases
        if (overrideEnvironment.Count == 0)
            return baseEnvironment;
        if (baseEnvironment.Count == 0)
            return overrideEnvironment;

        return Merge(baseEnvironment, overrideEnvironment);
    }
}
