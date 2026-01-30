namespace WorkflowEngine.Core.Models;

/// <summary>
/// Represents a single task within a workflow.
/// </summary>
public sealed class WorkflowTask
{
    /// <summary>
    /// Unique identifier for this task. Used in expressions: <c>${{ tasks.{Id}.output }}</c>
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name for this task. Falls back to Id if not specified.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Shell command to execute. Supports interpolation: <c>${{ env.VAR }}</c>, <c>${{ tasks.id.output }}</c>
    /// </summary>
    public required string Run { get; init; }

    /// <summary>
    /// Shell to use: bash, sh, zsh, pwsh, cmd.
    /// If null, inherits from workflow shell or uses platform default.
    /// </summary>
    public string? Shell { get; init; }

    /// <summary>
    /// Working directory for this task. Overrides workflow-level setting.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Task-specific environment variables. Take precedence over workflow-level vars.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Condition expression. Supports: success(), failure(), always(), cancelled().
    /// </summary>
    public string? If { get; init; }

    public TaskInput? Input { get; init; }
    public TaskOutputConfig? Output { get; init; }

    /// <summary>Timeout in milliseconds. Overrides workflow default.</summary>
    public int? TimeoutMs { get; init; }

    /// <summary>Continue workflow if this task fails. Default: false.</summary>
    public bool ContinueOnError { get; init; }

    /// <summary>Number of retry attempts on failure. Default: 0.</summary>
    public int RetryCount { get; init; }

    /// <summary>Delay between retries in milliseconds.</summary>
    public int RetryDelayMs { get; init; } = Defaults.RetryDelayMs;

    /// <summary>Task IDs that must complete before this task starts.</summary>
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    /// <summary>
    /// Gets the matrix configuration for generating multiple task instances.
    /// When specified, this task expands into multiple parallel tasks based on the matrix dimensions.
    /// </summary>
    public MatrixConfig? Matrix { get; init; }

    /// <summary>
    /// Gets the matrix values for this task instance if it was generated from a matrix expansion.
    /// </summary>
    public IReadOnlyDictionary<string, string>? MatrixValues { get; init; }

    /// <summary>
    /// Gets the Docker configuration for this task.
    /// Overrides workflow-level Docker config. Set to null to inherit from workflow.
    /// Set Container to empty string to disable Docker for this task and run locally.
    /// </summary>
    public DockerConfig? Docker { get; init; }

    /// <summary>
    /// Gets the SSH configuration for this task.
    /// Overrides workflow-level SSH config. Set to null to inherit from workflow.
    /// Set Disabled to true to disable SSH for this task and run locally.
    /// </summary>
    public SshConfig? Ssh { get; init; }

    public string DisplayName => Name ?? Id;
    public override string ToString() => $"Task[{DisplayName}]";
}

/// <summary>
/// Configuration for task input data.
/// </summary>
public sealed class TaskInput
{
    /// <summary>
    /// Gets the type of input.
    /// </summary>
    public InputType Type { get; init; } = InputType.None;

    /// <summary>
    /// Gets the input value or expression.
    /// </summary>
    /// <remarks>
    /// For <see cref="InputType.Pipe"/>, use an expression like <c>${{ tasks.previous.output }}</c>
    /// </remarks>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the file path for <see cref="InputType.File"/> inputs.
    /// </summary>
    public string? FilePath { get; init; }
}

/// <summary>
/// Configuration for capturing task output.
/// </summary>
public sealed class TaskOutputConfig
{
    /// <summary>
    /// Gets how to capture the output.
    /// </summary>
    public OutputType Type { get; init; } = OutputType.String;

    /// <summary>
    /// Gets the file path for <see cref="OutputType.File"/> outputs.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets whether to capture stderr separately.
    /// </summary>
    /// <value>Default is true.</value>
    public bool CaptureStderr { get; init; } = true;

    /// <summary>
    /// Maximum output size in bytes to prevent memory issues.
    /// </summary>
    public long MaxSizeBytes { get; init; } = Defaults.MaxOutputSizeBytes;
}
