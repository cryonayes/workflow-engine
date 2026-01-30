using YamlDotNet.Serialization;

namespace WorkflowEngine.Parsing.Dtos;

/// <summary>
/// Data transfer object for deserializing SSH configuration YAML.
/// All properties are nullable to support partial overrides at task level.
/// </summary>
internal sealed class SshDto
{
    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    [YamlMember(Alias = "user")]
    public string? User { get; set; }

    [YamlMember(Alias = "port")]
    public int? Port { get; set; }

    [YamlMember(Alias = "privateKeyPath")]
    public string? PrivateKeyPath { get; set; }

    [YamlMember(Alias = "workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [YamlMember(Alias = "environment")]
    public Dictionary<string, string>? Environment { get; set; }

    [YamlMember(Alias = "strictHostKeyChecking")]
    public bool? StrictHostKeyChecking { get; set; }

    [YamlMember(Alias = "extraArgs")]
    public List<string>? ExtraArgs { get; set; }

    [YamlMember(Alias = "disabled")]
    public bool? Disabled { get; set; }

    [YamlMember(Alias = "connectionTimeoutSeconds")]
    public int? ConnectionTimeoutSeconds { get; set; }

    /// <summary>Returns true if any property is set (for detecting partial configs).</summary>
    internal bool HasAnyValue =>
        Host is not null ||
        User is not null ||
        Port is not null ||
        PrivateKeyPath is not null ||
        WorkingDirectory is not null ||
        Environment is not null ||
        StrictHostKeyChecking is not null ||
        ExtraArgs is not null ||
        Disabled is not null ||
        ConnectionTimeoutSeconds is not null;
}
