namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Represents a single trigger rule configuration.
/// </summary>
public sealed class TriggerRule
{
    /// <summary>
    /// Gets the unique name of the trigger.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the source platforms this trigger listens to.
    /// </summary>
    public required IReadOnlyList<TriggerSource> Sources { get; init; }

    /// <summary>
    /// Gets the type of trigger matching.
    /// </summary>
    public required TriggerType Type { get; init; }

    /// <summary>
    /// Gets the pattern for command or regex matching.
    /// Used when Type is Command or Pattern.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets the keywords for keyword-based matching.
    /// Used when Type is Keyword.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>
    /// Gets the path to the workflow file to execute.
    /// </summary>
    public required string WorkflowPath { get; init; }

    /// <summary>
    /// Gets the parameters to pass to the workflow.
    /// Supports placeholders like {project}, {username}, etc.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the response template for replying to the trigger source.
    /// Supports placeholders like {runId}, {project}, etc.
    /// </summary>
    public string? ResponseTemplate { get; init; }

    /// <summary>
    /// Gets the cooldown duration between trigger activations.
    /// </summary>
    public TimeSpan? Cooldown { get; init; }

    /// <summary>
    /// Gets whether this trigger is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <inheritdoc />
    public override string ToString() => $"TriggerRule[{Name}:{Type}]";
}
