namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Represents the result of matching an incoming message against trigger rules.
/// </summary>
public sealed class TriggerMatchResult
{
    private TriggerMatchResult() { }

    /// <summary>
    /// Gets whether the match was successful.
    /// </summary>
    public bool IsMatch { get; private init; }

    /// <summary>
    /// Gets the trigger rule that matched.
    /// </summary>
    public TriggerRule? Rule { get; private init; }

    /// <summary>
    /// Gets the incoming message that was matched.
    /// </summary>
    public IncomingMessage? Message { get; private init; }

    /// <summary>
    /// Gets the captured parameters from the match.
    /// </summary>
    public IReadOnlyDictionary<string, string> Captures { get; private init; } = new Dictionary<string, string>();

    /// <summary>
    /// Creates a successful match result.
    /// </summary>
    public static TriggerMatchResult Success(
        TriggerRule rule,
        IncomingMessage message,
        IReadOnlyDictionary<string, string> captures)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(captures);

        return new TriggerMatchResult
        {
            IsMatch = true,
            Rule = rule,
            Message = message,
            Captures = captures
        };
    }

    /// <summary>
    /// Creates a failed match result.
    /// </summary>
    public static TriggerMatchResult NoMatch() => new() { IsMatch = false };
}
