using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Services;

/// <summary>
/// Resolves template placeholders in strings.
/// </summary>
public sealed class TemplateResolver : ITemplateResolver
{
    /// <inheritdoc />
    public string Resolve(
        string template,
        IReadOnlyDictionary<string, string> captures,
        IncomingMessage message,
        IReadOnlyDictionary<string, string>? additionalValues = null)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var result = template;

        // Replace captured values
        foreach (var (key, value) in captures)
        {
            result = result.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        // Replace additional values (e.g., runId)
        if (additionalValues is not null)
        {
            foreach (var (key, value) in additionalValues)
            {
                result = result.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Replace standard message context placeholders
        result = ReplaceMessagePlaceholders(result, message);

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ResolveParameters(
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<string, string> captures,
        IncomingMessage message)
    {
        var resolved = new Dictionary<string, string>(parameters.Count);

        foreach (var (key, value) in parameters)
        {
            resolved[key] = Resolve(value, captures, message);
        }

        return resolved;
    }

    private static string ReplaceMessagePlaceholders(string template, IncomingMessage message)
    {
        return template
            .Replace("{username}", message.Username ?? "unknown", StringComparison.OrdinalIgnoreCase)
            .Replace("{userId}", message.UserId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{channelId}", message.ChannelId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{channelName}", message.ChannelName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{source}", message.Source.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{messageId}", message.MessageId, StringComparison.OrdinalIgnoreCase)
            .Replace("{text}", message.Text, StringComparison.OrdinalIgnoreCase);
    }
}
