using YamlDotNet.Serialization;

namespace WorkflowEngine.Parsing.Dtos;

/// <summary>
/// Data transfer object for deserializing webhook YAML.
/// </summary>
internal sealed class WebhookDto
{
    [YamlMember(Alias = "provider")]
    public string? Provider { get; set; }

    [YamlMember(Alias = "url")]
    public string? Url { get; set; }

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "events")]
    public List<string>? Events { get; set; }

    [YamlMember(Alias = "headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [YamlMember(Alias = "options")]
    public Dictionary<string, string>? Options { get; set; }

    [YamlMember(Alias = "timeoutMs")]
    public int? TimeoutMs { get; set; }

    [YamlMember(Alias = "retryCount")]
    public int? RetryCount { get; set; }
}
