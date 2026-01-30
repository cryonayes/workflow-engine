using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Matching;

/// <summary>
/// Matcher for keyword-based triggers.
/// </summary>
public sealed class KeywordMatcher : ITypedTriggerMatcher
{
    private const string CaptureKey = "keyword";

    /// <inheritdoc />
    public TriggerType Type => TriggerType.Keyword;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? TryMatch(IncomingMessage message, TriggerRule rule)
    {
        if (rule.Keywords.Count == 0)
            return null;

        var matchedKeyword = rule.Keywords
            .FirstOrDefault(k => message.Text.Contains(k, StringComparison.OrdinalIgnoreCase));

        return matchedKeyword is not null
            ? new Dictionary<string, string> { [CaptureKey] = matchedKeyword }
            : null;
    }
}
