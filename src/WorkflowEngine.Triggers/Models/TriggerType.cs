namespace WorkflowEngine.Triggers.Models;

/// <summary>
/// Represents the type of trigger matching to perform.
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// Command-based trigger (e.g., "/build {project}").
    /// </summary>
    Command,

    /// <summary>
    /// Regex pattern-based trigger with named captures.
    /// </summary>
    Pattern,

    /// <summary>
    /// Keyword detection trigger.
    /// </summary>
    Keyword
}
