using System.Text.RegularExpressions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Storage;

/// <summary>
/// Maps trigger DTOs to domain models.
/// </summary>
internal static partial class TriggerConfigMapper
{
    public static TriggerConfig Map(TriggerConfigDto dto, string configPath) => new()
    {
        Credentials = MapCredentials(dto.Credentials),
        HttpServer = MapHttpServer(dto.HttpServer),
        Triggers = dto.Triggers?.Select(MapTriggerRule).ToList() ?? [],
        ConfigPath = configPath
    };

    private static CredentialsConfig MapCredentials(CredentialsDto? dto)
    {
        if (dto is null)
            return new CredentialsConfig();

        return new CredentialsConfig
        {
            Telegram = dto.Telegram is not null
                ? new TelegramCredentials { BotToken = dto.Telegram.BotToken ?? string.Empty }
                : null,
            Discord = dto.Discord is not null
                ? new DiscordCredentials { BotToken = dto.Discord.BotToken ?? string.Empty }
                : null,
            Slack = dto.Slack is not null
                ? new SlackCredentials
                {
                    AppToken = dto.Slack.AppToken ?? string.Empty,
                    SigningSecret = dto.Slack.SigningSecret ?? string.Empty
                }
                : null
        };
    }

    private static HttpServerConfig MapHttpServer(HttpServerDto? dto) => new()
    {
        Port = dto?.Port ?? 8080,
        Host = dto?.Host ?? "0.0.0.0",
        EnableHttps = dto?.EnableHttps ?? false,
        CertificatePath = dto?.CertificatePath,
        CertificatePassword = dto?.CertificatePassword
    };

    private static TriggerRule MapTriggerRule(TriggerRuleDto dto) => new()
    {
        Name = dto.Name ?? throw new InvalidOperationException("Trigger name is required"),
        Sources = ParseSources(dto.Source),
        Type = ParseTriggerType(dto.Type),
        Pattern = dto.Pattern,
        Keywords = dto.Keywords ?? [],
        WorkflowPath = dto.Workflow ?? throw new InvalidOperationException($"Trigger '{dto.Name}' must specify a workflow"),
        Parameters = dto.Parameters ?? new Dictionary<string, string>(),
        ResponseTemplate = dto.ResponseTemplate,
        Cooldown = ParseDuration(dto.Cooldown),
        Enabled = dto.Enabled ?? true
    };

    private static IReadOnlyList<TriggerSource> ParseSources(object? source)
    {
        return source switch
        {
            null => [TriggerSource.Http],
            string str when str.Equals("all", StringComparison.OrdinalIgnoreCase) =>
                Enum.GetValues<TriggerSource>().ToList(),
            string str => [ParseSingleSource(str)],
            IEnumerable<object> list => list.Select(s => ParseSingleSource(s?.ToString() ?? string.Empty)).ToList(),
            _ => [TriggerSource.Http]
        };
    }

    private static TriggerSource ParseSingleSource(string source) =>
        source.ToLowerInvariant() switch
        {
            "telegram" => TriggerSource.Telegram,
            "discord" => TriggerSource.Discord,
            "slack" => TriggerSource.Slack,
            "http" => TriggerSource.Http,
            _ => throw new InvalidOperationException($"Unknown trigger source: {source}")
        };

    private static TriggerType ParseTriggerType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "command" => TriggerType.Command,
            "pattern" => TriggerType.Pattern,
            "keyword" => TriggerType.Keyword,
            null => TriggerType.Command,
            _ => throw new InvalidOperationException($"Unknown trigger type: {type}")
        };

    private static TimeSpan? ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration))
            return null;

        var match = DurationRegex().Match(duration);
        if (!match.Success)
            return null;

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "ms" => TimeSpan.FromMilliseconds(value),
            "s" or "" => TimeSpan.FromSeconds(value),
            "m" => TimeSpan.FromMinutes(value),
            "h" => TimeSpan.FromHours(value),
            _ => TimeSpan.FromSeconds(value)
        };
    }

    [GeneratedRegex(@"^(\d+)(ms|s|m|h)?$", RegexOptions.IgnoreCase)]
    private static partial Regex DurationRegex();
}
