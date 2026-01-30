using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Strategies;

/// <summary>
/// Execution strategy for running commands inside Docker containers.
/// </summary>
public sealed class DockerExecutionStrategy : IExecutionStrategy
{
    private readonly IDockerCommandBuilder _dockerBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerExecutionStrategy"/> class.
    /// </summary>
    public DockerExecutionStrategy(IDockerCommandBuilder dockerBuilder)
    {
        _dockerBuilder = dockerBuilder ?? throw new ArgumentNullException(nameof(dockerBuilder));
    }

    /// <inheritdoc />
    public int Priority => 20;

    /// <inheritdoc />
    public string Name => "Docker";

    /// <inheritdoc />
    public bool CanHandle(Workflow workflow, WorkflowTask task)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(task);

        return _dockerBuilder.GetEffectiveConfig(workflow, task) is not null;
    }

    /// <inheritdoc />
    public ExecutionConfig BuildConfig(string command, WorkflowTask task, WorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var dockerConfig = _dockerBuilder.GetEffectiveConfig(context.Workflow, task)
            ?? throw new InvalidOperationException("Docker config is not available");

        // For Docker execution, use only declared environment (workflow + CLI options),
        // not host system environment variables which could override container settings
        var dockerEnvVars = EnvironmentMerger.Merge(context.DeclaredEnvironment, task.Environment);
        var (executable, args) = _dockerBuilder.BuildCommand(dockerConfig, command, dockerEnvVars, task.Shell);

        return new ExecutionConfig(
            executable,
            args,
            Directory.GetCurrentDirectory(),
            env =>
            {
                if (!string.IsNullOrWhiteSpace(dockerConfig.Host))
                    env["DOCKER_HOST"] = dockerConfig.Host;
            });
    }
}
