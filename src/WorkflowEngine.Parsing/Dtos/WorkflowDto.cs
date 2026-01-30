using YamlDotNet.Serialization;

namespace WorkflowEngine.Parsing.Dtos;

/// <summary>
/// Data transfer object for deserializing workflow YAML.
/// </summary>
internal sealed class WorkflowDto
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "environment")]
    public Dictionary<string, string>? Environment { get; set; }

    [YamlMember(Alias = "workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [YamlMember(Alias = "tasks")]
    public List<TaskDto>? Tasks { get; set; }

    [YamlMember(Alias = "defaultTimeoutMs")]
    public int? DefaultTimeoutMs { get; set; }

    [YamlMember(Alias = "maxParallelism")]
    public int? MaxParallelism { get; set; }

    [YamlMember(Alias = "webhooks")]
    public List<WebhookDto>? Webhooks { get; set; }

    [YamlMember(Alias = "docker")]
    public DockerDto? Docker { get; set; }

    [YamlMember(Alias = "ssh")]
    public SshDto? Ssh { get; set; }

    [YamlMember(Alias = "watch")]
    public WatchDto? Watch { get; set; }

    [YamlMember(Alias = "shell")]
    public string? Shell { get; set; }
}

/// <summary>
/// Data transfer object for deserializing task YAML.
/// </summary>
internal sealed class TaskDto
{
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "run")]
    public string? Run { get; set; }

    [YamlMember(Alias = "shell")]
    public string? Shell { get; set; }

    [YamlMember(Alias = "workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [YamlMember(Alias = "environment")]
    public Dictionary<string, string>? Environment { get; set; }

    [YamlMember(Alias = "if")]
    public string? If { get; set; }

    [YamlMember(Alias = "input")]
    public TaskInputDto? Input { get; set; }

    [YamlMember(Alias = "output")]
    public TaskOutputDto? Output { get; set; }

    [YamlMember(Alias = "timeoutMs")]
    public int? TimeoutMs { get; set; }

    [YamlMember(Alias = "continueOnError")]
    public bool? ContinueOnError { get; set; }

    [YamlMember(Alias = "retryCount")]
    public int? RetryCount { get; set; }

    [YamlMember(Alias = "retryDelayMs")]
    public int? RetryDelayMs { get; set; }

    [YamlMember(Alias = "dependsOn")]
    public List<string>? DependsOn { get; set; }

    [YamlMember(Alias = "matrix")]
    public Dictionary<string, object>? Matrix { get; set; }

    [YamlMember(Alias = "docker")]
    public DockerDto? Docker { get; set; }

    [YamlMember(Alias = "ssh")]
    public SshDto? Ssh { get; set; }
}

/// <summary>
/// Data transfer object for task input configuration.
/// </summary>
internal sealed class TaskInputDto
{
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "value")]
    public string? Value { get; set; }

    [YamlMember(Alias = "filePath")]
    public string? FilePath { get; set; }
}

/// <summary>
/// Data transfer object for task output configuration.
/// </summary>
internal sealed class TaskOutputDto
{
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "filePath")]
    public string? FilePath { get; set; }

    [YamlMember(Alias = "captureStderr")]
    public bool? CaptureStderr { get; set; }

    [YamlMember(Alias = "maxSizeBytes")]
    public long? MaxSizeBytes { get; set; }
}
