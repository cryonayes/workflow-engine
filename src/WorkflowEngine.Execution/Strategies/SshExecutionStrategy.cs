using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Strategies;

/// <summary>
/// Execution strategy for running commands on remote hosts via SSH.
/// Has the highest priority (checked first).
/// </summary>
public sealed class SshExecutionStrategy : IExecutionStrategy
{
    private readonly ISshCommandBuilder _sshBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SshExecutionStrategy"/> class.
    /// </summary>
    public SshExecutionStrategy(ISshCommandBuilder sshBuilder)
    {
        _sshBuilder = sshBuilder ?? throw new ArgumentNullException(nameof(sshBuilder));
    }

    /// <inheritdoc />
    public int Priority => 10;

    /// <inheritdoc />
    public string Name => "SSH";

    /// <inheritdoc />
    public bool CanHandle(Workflow workflow, WorkflowTask task)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(task);

        return _sshBuilder.GetEffectiveConfig(workflow, task) is not null;
    }

    /// <inheritdoc />
    public ExecutionConfig BuildConfig(string command, WorkflowTask task, WorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var sshConfig = _sshBuilder.GetEffectiveConfig(context.Workflow, task)
            ?? throw new InvalidOperationException("SSH config is not available");

        // For SSH execution, use only declared environment (workflow + CLI options)
        var sshEnvVars = EnvironmentMerger.Merge(context.DeclaredEnvironment, task.Environment);
        var (executable, args) = _sshBuilder.BuildCommand(sshConfig, command, sshEnvVars, task.Shell);

        return new ExecutionConfig(
            executable,
            args,
            Directory.GetCurrentDirectory(),
            _ => { }); // No local env vars needed for SSH
    }
}
