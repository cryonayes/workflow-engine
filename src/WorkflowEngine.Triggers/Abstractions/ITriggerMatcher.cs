using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Interface for matching incoming messages against trigger rules.
/// </summary>
public interface ITriggerMatcher
{
    /// <summary>
    /// Attempts to match an incoming message against a list of trigger rules.
    /// </summary>
    /// <param name="message">The incoming message to match.</param>
    /// <param name="rules">The trigger rules to match against.</param>
    /// <returns>The match result, or null if no match found.</returns>
    TriggerMatchResult? Match(IncomingMessage message, IReadOnlyList<TriggerRule> rules);
}

/// <summary>
/// Interface for individual trigger type matchers.
/// </summary>
public interface ITypedTriggerMatcher
{
    /// <summary>
    /// Gets the trigger type this matcher handles.
    /// </summary>
    TriggerType Type { get; }

    /// <summary>
    /// Attempts to match an incoming message against a single trigger rule.
    /// </summary>
    /// <param name="message">The incoming message to match.</param>
    /// <param name="rule">The trigger rule to match against.</param>
    /// <returns>Dictionary of captures if matched, null if not matched.</returns>
    IReadOnlyDictionary<string, string>? TryMatch(IncomingMessage message, TriggerRule rule);
}
