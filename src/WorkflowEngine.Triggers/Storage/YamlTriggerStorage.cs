using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WorkflowEngine.Triggers.Storage;

/// <summary>
/// Loads trigger configuration from YAML files.
/// </summary>
public sealed class YamlTriggerStorage : ITriggerStorage
{
    private const string DefaultConfigFileName = "triggers.yaml";
    private const string ConfigDirectory = ".workflow-engine";

    private readonly IDeserializer _deserializer;
    private readonly ILogger<YamlTriggerStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the YamlTriggerStorage.
    /// </summary>
    public YamlTriggerStorage(ILogger<YamlTriggerStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <inheritdoc />
    public async Task<TriggerConfig> LoadAsync(string configPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(configPath);

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Trigger configuration not found: {configPath}", configPath);
        }

        _logger.LogDebug("Loading trigger configuration from {ConfigPath}", configPath);

        var yaml = await File.ReadAllTextAsync(configPath, cancellationToken);
        yaml = EnvironmentVariableExpander.Expand(yaml);

        var dto = _deserializer.Deserialize<TriggerConfigDto>(yaml)
            ?? throw new InvalidOperationException("Failed to parse trigger configuration");

        var config = TriggerConfigMapper.Map(dto, configPath);

        _logger.LogInformation("Loaded {TriggerCount} triggers from {ConfigPath}",
            config.Triggers.Count, configPath);

        return config;
    }

    /// <inheritdoc />
    public string GetDefaultConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ConfigDirectory, DefaultConfigFileName);
    }
}
