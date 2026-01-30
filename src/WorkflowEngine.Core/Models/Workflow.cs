namespace WorkflowEngine.Core.Models;

/// <summary>
/// Represents a complete workflow definition containing tasks to be executed.
/// </summary>
public sealed class Workflow
{
    /// <summary>
    /// Gets the unique identifier for this workflow definition.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets the human-readable name of the workflow.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets an optional description of what this workflow does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Environment variables available to all tasks. Task-level vars take precedence.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Working directory for task execution. Defaults to current directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Tasks to execute. Execution order is determined by dependencies.
    /// </summary>
    public required IReadOnlyList<WorkflowTask> Tasks { get; init; }

    /// <summary>
    /// Default timeout in milliseconds for tasks that don't specify their own.
    /// </summary>
    public int DefaultTimeoutMs { get; init; } = Defaults.TimeoutMs;

    /// <summary>
    /// Gets the maximum degree of parallelism for concurrent task execution.
    /// </summary>
    /// <value>Default is -1 (unlimited, based on available CPU cores).</value>
    public int MaxParallelism { get; init; } = -1;

    /// <summary>
    /// Gets the webhook configurations for this workflow.
    /// </summary>
    public IReadOnlyList<WebhookConfig> Webhooks { get; init; } = [];

    /// <summary>
    /// Gets the Docker configuration for executing tasks inside a container.
    /// When specified, all tasks will be executed via docker exec unless overridden at task level.
    /// </summary>
    public DockerConfig? Docker { get; init; }

    /// <summary>
    /// Gets the SSH configuration for executing tasks on a remote machine.
    /// When specified, all tasks will be executed via SSH unless overridden at task level.
    /// </summary>
    public SshConfig? Ssh { get; init; }

    /// <summary>
    /// Gets the watch configuration for file-based workflow triggering.
    /// </summary>
    public WatchConfig? Watch { get; init; }

    /// <summary>
    /// Gets the default shell to use for tasks that don't specify their own.
    /// Supported values: bash, sh, zsh, pwsh, cmd.
    /// </summary>
    public string? Shell { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"Workflow[{Name}] ({Tasks.Count} tasks)";
}
