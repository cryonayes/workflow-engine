using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Events;

/// <summary>
/// Base record for all trigger events.
/// </summary>
public abstract record TriggerEvent(DateTimeOffset Timestamp);

/// <summary>
/// Base record for rule-specific trigger events.
/// </summary>
public abstract record RuleTriggerEvent(
    string RuleName,
    TriggerSource Source,
    DateTimeOffset Timestamp
) : TriggerEvent(Timestamp);

/// <summary>
/// Event raised when the trigger service starts.
/// </summary>
public sealed record TriggerServiceStartedEvent(
    int ListenerCount,
    int RuleCount
) : TriggerEvent(DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when the trigger service stops.
/// </summary>
public sealed record TriggerServiceStoppedEvent(
    string? Reason = null
) : TriggerEvent(DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a listener connects.
/// </summary>
public sealed record ListenerConnectedEvent(
    TriggerSource Source
) : TriggerEvent(DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a listener disconnects.
/// </summary>
public sealed record ListenerDisconnectedEvent(
    TriggerSource Source,
    string? Reason = null
) : TriggerEvent(DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a message is received.
/// </summary>
public sealed record MessageReceivedEvent(
    TriggerSource Source,
    string MessageId,
    string TextPreview,
    string? Username
) : TriggerEvent(DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a trigger rule matches.
/// </summary>
public sealed record TriggerMatchedEvent(
    string RuleName,
    TriggerSource Source,
    string MessageId,
    IReadOnlyDictionary<string, string> Captures
) : RuleTriggerEvent(RuleName, Source, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a workflow is dispatched from a trigger.
/// </summary>
public sealed record TriggerDispatchedEvent(
    string RuleName,
    TriggerSource Source,
    string WorkflowPath,
    string RunId,
    string? Username
) : RuleTriggerEvent(RuleName, Source, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a trigger dispatch fails.
/// </summary>
public sealed record TriggerDispatchFailedEvent(
    string RuleName,
    TriggerSource Source,
    string WorkflowPath,
    string ErrorMessage
) : RuleTriggerEvent(RuleName, Source, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a response is sent to a trigger source.
/// </summary>
public sealed record ResponseSentEvent(
    TriggerSource Source,
    string MessageId,
    string Response
) : TriggerEvent(DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when a trigger is rate-limited by cooldown.
/// </summary>
public sealed record TriggerCooldownEvent(
    string RuleName,
    TriggerSource Source,
    TimeSpan RemainingCooldown
) : RuleTriggerEvent(RuleName, Source, DateTimeOffset.UtcNow);

/// <summary>
/// Event raised when an error occurs in the trigger system.
/// </summary>
public sealed record TriggerErrorEvent(
    string Component,
    string ErrorMessage,
    Exception? Exception = null
) : TriggerEvent(DateTimeOffset.UtcNow);
