namespace WorkflowEngine.Triggers.Storage;

/// <summary>
/// Root DTO for triggers.yaml configuration.
/// </summary>
internal sealed class TriggerConfigDto
{
    public CredentialsDto? Credentials { get; set; }
    public HttpServerDto? HttpServer { get; set; }
    public List<TriggerRuleDto>? Triggers { get; set; }
}

/// <summary>
/// Credentials section DTO.
/// </summary>
internal sealed class CredentialsDto
{
    public TelegramCredentialsDto? Telegram { get; set; }
    public DiscordCredentialsDto? Discord { get; set; }
    public SlackCredentialsDto? Slack { get; set; }
}

/// <summary>
/// Telegram credentials DTO.
/// </summary>
internal sealed class TelegramCredentialsDto
{
    public string? BotToken { get; set; }
}

/// <summary>
/// Discord credentials DTO.
/// </summary>
internal sealed class DiscordCredentialsDto
{
    public string? BotToken { get; set; }
}

/// <summary>
/// Slack credentials DTO.
/// </summary>
internal sealed class SlackCredentialsDto
{
    public string? AppToken { get; set; }
    public string? SigningSecret { get; set; }
}

/// <summary>
/// HTTP server section DTO.
/// </summary>
internal sealed class HttpServerDto
{
    public int? Port { get; set; }
    public string? Host { get; set; }
    public bool? EnableHttps { get; set; }
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
}

/// <summary>
/// Individual trigger rule DTO.
/// </summary>
internal sealed class TriggerRuleDto
{
    public string? Name { get; set; }
    public object? Source { get; set; } // Can be string, list, or "all"
    public string? Type { get; set; }
    public string? Pattern { get; set; }
    public List<string>? Keywords { get; set; }
    public string? Workflow { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public string? ResponseTemplate { get; set; }
    public string? Cooldown { get; set; }
    public bool? Enabled { get; set; }
}
