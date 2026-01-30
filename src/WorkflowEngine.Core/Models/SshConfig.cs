namespace WorkflowEngine.Core.Models;

/// <summary>
/// Configuration for executing tasks on a remote machine via SSH.
/// Supports workflow-level defaults with task-level overrides.
/// </summary>
public sealed class SshConfig : IRemoteExecutionConfig<SshConfig>
{
    /// <summary>
    /// Gets the SSH host address or hostname. Required for execution.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Gets the SSH username. Required for execution.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Gets the SSH port. Default is 22.
    /// </summary>
    public int Port { get; init; } = 22;

    /// <summary>
    /// Gets the path to the SSH private key file.
    /// </summary>
    public string? PrivateKeyPath { get; init; }

    /// <summary>
    /// Gets the working directory on the remote host.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets environment variables to set on the remote host.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets whether to enable strict host key checking. Default is true.
    /// </summary>
    public bool StrictHostKeyChecking { get; init; } = true;

    /// <summary>
    /// Gets additional SSH command-line arguments.
    /// </summary>
    public IReadOnlyList<string>? ExtraArgs { get; init; }

    /// <summary>
    /// Gets whether SSH execution is explicitly disabled for this task.
    /// Use at task level to run locally even when workflow has SSH configured.
    /// </summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// Gets the connection timeout in seconds. Default is 30.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Returns true if SSH execution should be skipped.
    /// </summary>
    public bool IsDisabled => Disabled || string.IsNullOrWhiteSpace(Host);

    /// <summary>
    /// Returns true if this config has valid connection details for execution.
    /// </summary>
    public bool IsValid => !Disabled && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(User);

    /// <inheritdoc />
    public override string ToString() => Disabled ? "SSH[disabled]" :
        string.IsNullOrWhiteSpace(Host) ? "SSH[partial]" : $"SSH[{User}@{Host}:{Port}]";

    /// <summary>
    /// Merges this config with a base config, using this config's values when set.
    /// </summary>
    /// <param name="baseConfig">The base configuration to merge with.</param>
    /// <returns>A merged SSH configuration.</returns>
    public SshConfig MergeWith(SshConfig? baseConfig)
    {
        if (baseConfig is null)
            return this;

        // If explicitly disabled, return disabled config
        if (Disabled)
            return this;

        return new SshConfig
        {
            Host = Host ?? baseConfig.Host,
            User = User ?? baseConfig.User,
            Port = Port != 22 ? Port : baseConfig.Port,
            PrivateKeyPath = PrivateKeyPath ?? baseConfig.PrivateKeyPath,
            WorkingDirectory = WorkingDirectory ?? baseConfig.WorkingDirectory,
            Environment = MergeEnvironment(baseConfig.Environment, Environment),
            StrictHostKeyChecking = StrictHostKeyChecking && baseConfig.StrictHostKeyChecking,
            ExtraArgs = ExtraArgs ?? baseConfig.ExtraArgs,
            ConnectionTimeoutSeconds = ConnectionTimeoutSeconds != 30 ? ConnectionTimeoutSeconds : baseConfig.ConnectionTimeoutSeconds,
            Disabled = false
        };
    }

    private static IReadOnlyDictionary<string, string> MergeEnvironment(
        IReadOnlyDictionary<string, string> baseEnv,
        IReadOnlyDictionary<string, string> overrideEnv)
    {
        if (overrideEnv.Count == 0)
            return baseEnv;
        if (baseEnv.Count == 0)
            return overrideEnv;

        var merged = new Dictionary<string, string>(baseEnv);
        foreach (var (key, value) in overrideEnv)
            merged[key] = value;
        return merged;
    }
}
