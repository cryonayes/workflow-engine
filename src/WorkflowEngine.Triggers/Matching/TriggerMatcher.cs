using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Matching;

/// <summary>
/// Coordinates trigger matching across all matcher types.
/// </summary>
public sealed class TriggerMatcher : ITriggerMatcher
{
    private readonly IReadOnlyDictionary<TriggerType, ITypedTriggerMatcher> _matchers;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldownTracker = new();
    private readonly ILogger<TriggerMatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the TriggerMatcher.
    /// </summary>
    public TriggerMatcher(
        IEnumerable<ITypedTriggerMatcher> matchers,
        ILogger<TriggerMatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(matchers);
        ArgumentNullException.ThrowIfNull(logger);

        _matchers = matchers.ToDictionary(m => m.Type);
        _logger = logger;
    }

    /// <inheritdoc />
    public TriggerMatchResult? Match(IncomingMessage message, IReadOnlyList<TriggerRule> rules)
    {
        foreach (var rule in rules)
        {
            if (!IsRuleApplicable(rule, message))
                continue;

            var captures = TryMatchRule(rule, message);
            if (captures is null)
                continue;

            RecordTrigger(rule);

            _logger.LogInformation(
                "Matched rule '{RuleName}' for message from {Source}",
                rule.Name, message.Source);

            return TriggerMatchResult.Success(rule, message, captures);
        }

        return null;
    }

    /// <summary>
    /// Gets the remaining cooldown time for a rule, if any.
    /// </summary>
    public TimeSpan? GetRemainingCooldown(TriggerRule rule)
    {
        if (rule.Cooldown is null or { Ticks: 0 })
            return null;

        if (!_cooldownTracker.TryGetValue(rule.Name, out var lastTriggerTime))
            return null;

        var elapsed = DateTimeOffset.UtcNow - lastTriggerTime;
        var remaining = rule.Cooldown.Value - elapsed;

        return remaining > TimeSpan.Zero ? remaining : null;
    }

    private bool IsRuleApplicable(TriggerRule rule, IncomingMessage message)
    {
        if (!rule.Enabled)
            return false;

        if (!rule.Sources.Contains(message.Source))
            return false;

        if (GetRemainingCooldown(rule) is not null)
        {
            _logger.LogDebug("Rule '{RuleName}' is on cooldown", rule.Name);
            return false;
        }

        return true;
    }

    private IReadOnlyDictionary<string, string>? TryMatchRule(TriggerRule rule, IncomingMessage message)
    {
        if (!_matchers.TryGetValue(rule.Type, out var matcher))
        {
            _logger.LogWarning("No matcher registered for type {TriggerType}", rule.Type);
            return null;
        }

        return matcher.TryMatch(message, rule);
    }

    private void RecordTrigger(TriggerRule rule)
    {
        if (rule.Cooldown is not null and not { Ticks: 0 })
        {
            _cooldownTracker[rule.Name] = DateTimeOffset.UtcNow;
        }
    }
}
