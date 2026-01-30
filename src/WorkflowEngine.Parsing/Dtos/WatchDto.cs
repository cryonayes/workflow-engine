using YamlDotNet.Serialization;

namespace WorkflowEngine.Parsing.Dtos;

/// <summary>
/// Data transfer object for deserializing watch configuration YAML.
/// </summary>
internal sealed class WatchDto
{
    [YamlMember(Alias = "paths")]
    public List<string>? Paths { get; set; }

    [YamlMember(Alias = "ignore")]
    public List<string>? Ignore { get; set; }

    [YamlMember(Alias = "debounce")]
    public string? Debounce { get; set; }

    [YamlMember(Alias = "tasks")]
    public List<string>? Tasks { get; set; }

    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }

    [YamlMember(Alias = "runOnStart")]
    public bool? RunOnStart { get; set; }

    /// <summary>Returns true if any property is set.</summary>
    internal bool HasAnyValue =>
        Paths is not null ||
        Ignore is not null ||
        Debounce is not null ||
        Tasks is not null ||
        Enabled is not null ||
        RunOnStart is not null;
}
