namespace WorkflowEngine.Core.Models;

/// <summary>
/// Common interface for remote execution configurations (Docker, SSH, etc.).
/// Provides a consistent pattern for config merging and validation.
/// </summary>
/// <typeparam name="T">The config type for fluent merging.</typeparam>
public interface IRemoteExecutionConfig<T> where T : class, IRemoteExecutionConfig<T>
{
    /// <summary>
    /// Gets whether this execution method is explicitly disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets whether this config has valid settings for execution.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Merges this config with a base config, using this config's values when set.
    /// </summary>
    /// <param name="baseConfig">The base configuration to merge with.</param>
    /// <returns>A merged configuration.</returns>
    T MergeWith(T? baseConfig);
}

/// <summary>
/// Utility for resolving effective remote execution configurations.
/// </summary>
public static class ConfigMerger
{
    /// <summary>
    /// Gets the effective configuration by merging workflow and task configs.
    /// Returns null if no config is set or if execution is disabled.
    /// </summary>
    /// <typeparam name="T">The config type.</typeparam>
    /// <param name="workflowConfig">The workflow-level configuration.</param>
    /// <param name="taskConfig">The task-level configuration (overrides workflow).</param>
    /// <returns>The effective configuration, or null if disabled or not configured.</returns>
    public static T? GetEffectiveConfig<T>(T? workflowConfig, T? taskConfig)
        where T : class, IRemoteExecutionConfig<T>
    {
        // No config anywhere - return null
        if (workflowConfig is null && taskConfig is null)
            return null;

        // Task explicitly disables - return null
        if (taskConfig?.Disabled == true)
            return null;

        // Merge task config with workflow config (task overrides workflow)
        var effectiveConfig = taskConfig?.MergeWith(workflowConfig) ?? workflowConfig;

        // After merging, check if we have a valid config
        return effectiveConfig?.IsValid == true ? effectiveConfig : null;
    }
}
