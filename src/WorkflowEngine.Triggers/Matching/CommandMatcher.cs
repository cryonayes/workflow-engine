using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Infrastructure;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Matching;

/// <summary>
/// Matcher for command-based triggers (e.g., "/build {project}").
/// </summary>
public sealed partial class CommandMatcher : ITypedTriggerMatcher
{
    private readonly ConcurrentDictionary<string, Regex> _patternCache = new();

    /// <inheritdoc />
    public TriggerType Type => TriggerType.Command;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? TryMatch(IncomingMessage message, TriggerRule rule)
    {
        if (string.IsNullOrEmpty(rule.Pattern))
            return null;

        var regex = _patternCache.GetOrAdd(rule.Pattern, ConvertPatternToRegex);
        var match = regex.Match(message.Text);

        return match.Success ? match.ExtractNamedCaptures(regex) : null;
    }

    private static Regex ConvertPatternToRegex(string pattern)
    {
        // Step 1: Extract placeholders and replace with tokens
        var placeholders = new Dictionary<string, string>();
        var tokenized = PlaceholderRegex().Replace(pattern, m =>
        {
            var name = m.Groups[1].Value;
            var token = $"__PH{placeholders.Count}__";
            placeholders[token] = name;
            return token;
        });

        // Step 2: Escape special regex characters
        var escaped = Regex.Escape(tokenized);

        // Step 3: Replace tokens with named capture groups
        foreach (var (token, name) in placeholders)
        {
            escaped = escaped.Replace(token, $"(?<{name}>\\S+)");
        }

        // Step 4: Allow flexible whitespace at boundaries and between words
        var regexPattern = @"^\s*" + escaped.Replace(@"\ ", @"\s+") + @"\s*$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PlaceholderRegex();
}
