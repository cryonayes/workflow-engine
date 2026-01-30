namespace WorkflowEngine.Scheduling.Models;

/// <summary>
/// Represents a request to manually dispatch a workflow.
/// </summary>
public sealed class ManualDispatchRequest
{
    /// <summary>
    /// Gets the path to the workflow YAML file.
    /// </summary>
    public required string WorkflowPath { get; init; }

    /// <summary>
    /// Gets the input parameters to pass to the workflow as environment variables.
    /// </summary>
    public IReadOnlyDictionary<string, string> InputParameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets an optional reason for this manual dispatch.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets who triggered this dispatch (optional).
    /// </summary>
    public string? TriggeredBy { get; init; }
}
