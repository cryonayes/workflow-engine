using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Strategies;

/// <summary>
/// Execution strategy for running commands locally on the host machine.
/// This is the fallback strategy with the lowest priority.
/// </summary>
public sealed class LocalExecutionStrategy : IExecutionStrategy
{
    private readonly IShellProvider _shellProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalExecutionStrategy"/> class.
    /// </summary>
    public LocalExecutionStrategy(IShellProvider shellProvider)
    {
        _shellProvider = shellProvider ?? throw new ArgumentNullException(nameof(shellProvider));
    }

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public string Name => "Local";

    /// <inheritdoc />
    public bool CanHandle(Workflow workflow, WorkflowTask task)
    {
        // Local execution is the fallback - always handles
        return true;
    }

    /// <inheritdoc />
    public ExecutionConfig BuildConfig(string command, WorkflowTask task, WorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var envVars = EnvironmentMerger.Merge(context.Environment, task.Environment);
        var shell = GetShellConfig(task.Shell);
        var workingDir = task.WorkingDirectory ?? context.WorkingDirectory;

        return new ExecutionConfig(
            shell.Executable,
            shell.BuildArguments(command),
            workingDir,
            env =>
            {
                foreach (var (key, value) in envVars)
                    env[key] = value;
            });
    }

    private ShellConfiguration GetShellConfig(string? shellType)
    {
        var effectiveShell = string.IsNullOrWhiteSpace(shellType)
            ? _shellProvider.DefaultShellType
            : shellType;

        var config = _shellProvider.GetShellConfiguration(effectiveShell);
        if (config is not null)
            return config;

        // Fallback to platform default
        config = _shellProvider.GetShellConfiguration(_shellProvider.DefaultShellType);
        if (config is not null)
            return config;

        throw new InvalidOperationException(
            $"Shell '{effectiveShell}' is not supported. Available shells: {string.Join(", ", _shellProvider.SupportedShells)}");
    }
}
