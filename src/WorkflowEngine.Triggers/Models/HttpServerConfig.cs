namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Configuration for the embedded HTTP server.
/// </summary>
public sealed class HttpServerConfig
{
    /// <summary>
    /// Gets the port to listen on.
    /// </summary>
    public int Port { get; init; } = 8080;

    /// <summary>
    /// Gets the host to bind to.
    /// </summary>
    public string Host { get; init; } = "0.0.0.0";

    /// <summary>
    /// Gets whether to enable HTTPS.
    /// </summary>
    public bool EnableHttps { get; init; }

    /// <summary>
    /// Gets the path to the HTTPS certificate file.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets the HTTPS certificate password.
    /// </summary>
    public string? CertificatePassword { get; init; }
}
