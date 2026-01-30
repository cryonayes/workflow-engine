using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Interface for loading trigger configuration.
/// </summary>
public interface ITriggerStorage
{
    /// <summary>
    /// Loads the trigger configuration from the specified path.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded trigger configuration.</returns>
    Task<TriggerConfig> LoadAsync(string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default configuration file path.
    /// </summary>
    /// <returns>The default path to the triggers.yaml file.</returns>
    string GetDefaultConfigPath();
}
