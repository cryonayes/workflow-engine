using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Docker;

/// <summary>
/// Builds docker exec commands for container execution.
/// Supports merging workflow-level and task-level Docker configurations.
/// </summary>
public sealed class DockerCommandBuilder : IDockerCommandBuilder
{
    private const string DockerExecutable = "docker";

    // Default values for optional boolean flags
    private const bool DefaultInteractive = true;
    private const bool DefaultTty = false;
    private const bool DefaultPrivileged = false;

    private readonly IShellProvider _shellProvider;

    /// <summary>
    /// Initializes a new instance with the specified shell provider.
    /// </summary>
    /// <param name="shellProvider">Provider for shell configurations and platform defaults.</param>
    public DockerCommandBuilder(IShellProvider shellProvider)
    {
        _shellProvider = shellProvider ?? throw new ArgumentNullException(nameof(shellProvider));
    }

    /// <inheritdoc />
    public (string Executable, string[] Arguments) BuildCommand(
        DockerConfig config,
        string command,
        IReadOnlyDictionary<string, string> environment,
        string? shell = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(environment);

        if (!config.IsValid)
            throw new ArgumentException("Docker config must have a valid container", nameof(config));

        // Use provided shell or fall back to platform default from shell provider
        var shellToUse = string.IsNullOrWhiteSpace(shell) ? _shellProvider.DefaultShellType : shell;

        // Get shell configuration to use correct argument pattern (e.g., "-c" for bash, "-Command" for pwsh)
        var shellConfig = _shellProvider.GetShellConfiguration(shellToUse)
            ?? _shellProvider.GetShellConfiguration(_shellProvider.DefaultShellType);

        var args = BuildArguments(config, environment);
        args.Add(config.Container!);

        // Build shell command with correct arguments from shell configuration
        // ShellConfiguration.BuildArguments returns ["-c", "command"] or ["-Command", "command"] etc.
        if (shellConfig is not null)
        {
            args.Add(shellConfig.Executable);
            args.AddRange(shellConfig.BuildArguments(command));
        }
        else
        {
            // Fallback to basic shell invocation (should never happen with properly configured provider)
            args.AddRange([shellToUse, "-c", command]);
        }

        return (DockerExecutable, args.ToArray());
    }

    /// <inheritdoc />
    public DockerConfig? GetEffectiveConfig(Workflow workflow, WorkflowTask task)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(task);

        return ConfigMerger.GetEffectiveConfig(workflow.Docker, task.Docker);
    }

    private static List<string> BuildArguments(DockerConfig config, IReadOnlyDictionary<string, string> environment)
    {
        var args = new List<string> { "exec" };

        // Apply boolean flags with defaults
        if (config.Interactive ?? DefaultInteractive)
            args.Add("-i");
        if (config.Tty ?? DefaultTty)
            args.Add("-t");
        if (config.Privileged ?? DefaultPrivileged)
            args.Add("--privileged");

        AddOption(args, "-u", config.User);
        AddOption(args, "-w", config.WorkingDirectory);

        AddEnvironmentVariables(args, config.Environment);
        AddEnvironmentVariables(args, environment);

        if (config.ExtraArgs is not null)
            args.AddRange(config.ExtraArgs);

        return args;
    }

    private static void AddOption(List<string> args, string flag, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            args.Add(flag);
            args.Add(value);
        }
    }

    private static void AddEnvironmentVariables(List<string> args, IReadOnlyDictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            args.Add("-e");
            args.Add($"{key}={value}");
        }
    }
}
