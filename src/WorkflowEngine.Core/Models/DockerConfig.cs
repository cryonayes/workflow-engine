namespace WorkflowEngine.Core.Models;

/// <summary>
/// Configuration for executing tasks inside a Docker container via docker exec.
/// Supports workflow-level defaults with task-level overrides.
/// </summary>
public sealed class DockerConfig : IRemoteExecutionConfig<DockerConfig>
{
    /// <summary>Container name or ID to execute commands in. Required for execution.</summary>
    public string? Container { get; init; }

    /// <summary>User to run commands as (e.g., root, hunter, 1000:1000).</summary>
    public string? User { get; init; }

    /// <summary>Working directory inside the container.</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>Additional environment variables for the container.</summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>Enable interactive mode (-i flag) for stdin support. Default: true.</summary>
    public bool? Interactive { get; init; }

    /// <summary>Allocate pseudo-TTY (-t flag). Default: false.</summary>
    public bool? Tty { get; init; }

    /// <summary>Run in privileged mode. Default: false.</summary>
    public bool? Privileged { get; init; }

    /// <summary>Docker host URL (e.g., tcp://localhost:2375). Null uses default socket.</summary>
    public string? Host { get; init; }

    /// <summary>Additional docker exec arguments (e.g., --cap-add=NET_ADMIN).</summary>
    public IReadOnlyList<string>? ExtraArgs { get; init; }

    /// <summary>
    /// Explicitly disables Docker execution for this task when set to true.
    /// Use at task level to run locally even when workflow has Docker configured.
    /// </summary>
    public bool Disabled { get; init; }

    /// <summary>Returns true if Docker execution should be skipped.</summary>
    public bool IsDisabled => Disabled || string.IsNullOrWhiteSpace(Container);

    /// <summary>Returns true if this config has a valid container for execution.</summary>
    public bool IsValid => !Disabled && !string.IsNullOrWhiteSpace(Container);

    /// <inheritdoc />
    public override string ToString() => Disabled ? "Docker[disabled]" :
        string.IsNullOrWhiteSpace(Container) ? "Docker[partial]" : $"Docker[{Container}]";

    /// <summary>
    /// Merges this config with a base config, using this config's values when set.
    /// </summary>
    public DockerConfig MergeWith(DockerConfig? baseConfig)
    {
        if (baseConfig is null)
            return this;

        // If explicitly disabled, return disabled config
        if (Disabled)
            return this;

        return new DockerConfig
        {
            Container = Container ?? baseConfig.Container,
            User = User ?? baseConfig.User,
            WorkingDirectory = WorkingDirectory ?? baseConfig.WorkingDirectory,
            Environment = MergeEnvironment(baseConfig.Environment, Environment),
            Interactive = Interactive ?? baseConfig.Interactive,
            Tty = Tty ?? baseConfig.Tty,
            Privileged = Privileged ?? baseConfig.Privileged,
            Host = Host ?? baseConfig.Host,
            ExtraArgs = ExtraArgs ?? baseConfig.ExtraArgs,
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
