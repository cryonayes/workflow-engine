using System.Text;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Ssh;

/// <summary>
/// Builds SSH commands for remote execution.
/// Supports merging workflow-level and task-level SSH configurations.
/// </summary>
public sealed class SshCommandBuilder : ISshCommandBuilder
{
    private const string SshExecutable = "ssh";

    private readonly IShellProvider _shellProvider;

    /// <summary>
    /// Initializes a new instance with the specified shell provider.
    /// </summary>
    /// <param name="shellProvider">Provider for shell configurations and platform defaults.</param>
    public SshCommandBuilder(IShellProvider shellProvider)
    {
        _shellProvider = shellProvider ?? throw new ArgumentNullException(nameof(shellProvider));
    }

    /// <inheritdoc />
    public (string Executable, string[] Arguments) BuildCommand(
        SshConfig config,
        string command,
        IReadOnlyDictionary<string, string> environment,
        string? shell = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(environment);

        if (!config.IsValid)
            throw new ArgumentException("SSH config must have a valid host and user", nameof(config));

        var args = BuildSshArguments(config);
        args.Add($"{config.User}@{config.Host}");

        // Build the remote command with environment variables and working directory
        var remoteCommand = BuildRemoteCommand(config, command, environment, shell);
        args.Add(remoteCommand);

        return (SshExecutable, args.ToArray());
    }

    /// <inheritdoc />
    public SshConfig? GetEffectiveConfig(Workflow workflow, WorkflowTask task)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(task);

        return ConfigMerger.GetEffectiveConfig(workflow.Ssh, task.Ssh);
    }

    private static List<string> BuildSshArguments(SshConfig config)
    {
        var args = new List<string>();

        // Batch mode - non-interactive, prevents password prompts
        args.Add("-o");
        args.Add("BatchMode=yes");

        // Connection timeout
        args.Add("-o");
        args.Add($"ConnectTimeout={config.ConnectionTimeoutSeconds}");

        // Strict host key checking
        args.Add("-o");
        args.Add($"StrictHostKeyChecking={(config.StrictHostKeyChecking ? "yes" : "no")}");

        // Private key
        if (!string.IsNullOrWhiteSpace(config.PrivateKeyPath))
        {
            args.Add("-i");
            args.Add(ExpandPath(config.PrivateKeyPath));
        }

        // Port (only add if non-default)
        if (config.Port != 22)
        {
            args.Add("-p");
            args.Add(config.Port.ToString());
        }

        // Extra arguments
        if (config.ExtraArgs is not null)
        {
            args.AddRange(config.ExtraArgs);
        }

        return args;
    }

    private string BuildRemoteCommand(
        SshConfig config,
        string command,
        IReadOnlyDictionary<string, string> environment,
        string? shell)
    {
        var sb = new StringBuilder();

        // Export environment variables
        var allEnv = MergeEnvironment(config.Environment, environment);
        foreach (var (key, value) in allEnv)
        {
            var escapedValue = EscapeForShell(value);
            sb.Append($"export {key}={escapedValue} && ");
        }

        // Change to working directory
        if (!string.IsNullOrWhiteSpace(config.WorkingDirectory))
        {
            sb.Append($"cd {EscapeForShell(config.WorkingDirectory)} && ");
        }

        // Get the shell to use on the remote host
        var shellToUse = string.IsNullOrWhiteSpace(shell) ? _shellProvider.DefaultShellType : shell;
        var shellConfig = _shellProvider.GetShellConfiguration(shellToUse)
            ?? _shellProvider.GetShellConfiguration(_shellProvider.DefaultShellType);

        // Build the shell command
        // We wrap the command in the appropriate shell invocation
        if (shellConfig is not null)
        {
            var shellArgs = shellConfig.BuildArguments(command);
            // Typically this will be something like: bash -c "command"
            sb.Append(shellConfig.Executable);
            foreach (var arg in shellArgs)
            {
                sb.Append(' ');
                sb.Append(EscapeForShell(arg));
            }
        }
        else
        {
            // Fallback to basic bash invocation
            sb.Append($"bash -c {EscapeForShell(command)}");
        }

        return sb.ToString();
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

    private static string EscapeForShell(string value)
    {
        // Use single quotes and escape any embedded single quotes
        // 'value' -> 'val'\''ue' (end quote, escaped quote, start quote)
        var escaped = value.Replace("'", "'\"'\"'");
        return $"'{escaped}'";
    }

    private static string ExpandPath(string path)
    {
        // Expand ~ to home directory
        if (path.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        return path;
    }
}
