using YamlDotNet.Serialization;

namespace WorkflowEngine.Parsing.Dtos;

/// <summary>
/// Data transfer object for deserializing Docker configuration YAML.
/// All properties are nullable to support partial overrides at task level.
/// </summary>
internal sealed class DockerDto
{
    [YamlMember(Alias = "container")]
    public string? Container { get; set; }

    [YamlMember(Alias = "user")]
    public string? User { get; set; }

    [YamlMember(Alias = "workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [YamlMember(Alias = "environment")]
    public Dictionary<string, string>? Environment { get; set; }

    [YamlMember(Alias = "interactive")]
    public bool? Interactive { get; set; }

    [YamlMember(Alias = "tty")]
    public bool? Tty { get; set; }

    [YamlMember(Alias = "privileged")]
    public bool? Privileged { get; set; }

    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    [YamlMember(Alias = "extraArgs")]
    public List<string>? ExtraArgs { get; set; }

    [YamlMember(Alias = "disabled")]
    public bool? Disabled { get; set; }

    /// <summary>Returns true if any property is set (for detecting partial configs).</summary>
    internal bool HasAnyValue =>
        Container is not null ||
        User is not null ||
        WorkingDirectory is not null ||
        Environment is not null ||
        Interactive is not null ||
        Tty is not null ||
        Privileged is not null ||
        Host is not null ||
        ExtraArgs is not null ||
        Disabled is not null;
}
