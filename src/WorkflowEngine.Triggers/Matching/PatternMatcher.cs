using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Infrastructure;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Matching;

/// <summary>
/// Matcher for regex pattern-based triggers with named captures.
/// </summary>
public sealed class PatternMatcher : ITypedTriggerMatcher
{
    private readonly ConcurrentDictionary<string, Regex> _patternCache = new();

    /// <inheritdoc />
    public TriggerType Type => TriggerType.Pattern;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? TryMatch(IncomingMessage message, TriggerRule rule)
    {
        if (string.IsNullOrEmpty(rule.Pattern))
            return null;

        var regex = _patternCache.GetOrAdd(
            $"{rule.Name}:{rule.Pattern}",
            _ => new Regex(rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));

        var match = regex.Match(message.Text);

        return match.Success ? match.ExtractNamedCaptures(regex) : null;
    }
}
