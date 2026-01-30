namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Root configuration for the trigger service.
/// </summary>
public sealed class TriggerConfig
{
    /// <summary>
    /// Gets the credentials for various trigger sources.
    /// </summary>
    public CredentialsConfig Credentials { get; init; } = new();

    /// <summary>
    /// Gets the HTTP server configuration.
    /// </summary>
    public HttpServerConfig HttpServer { get; init; } = new();

    /// <summary>
    /// Gets the list of trigger rules.
    /// </summary>
    public IReadOnlyList<TriggerRule> Triggers { get; init; } = [];

    /// <summary>
    /// Gets the path to the configuration file this was loaded from.
    /// </summary>
    public string? ConfigPath { get; init; }

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    public static TriggerConfig Empty => new();
}
