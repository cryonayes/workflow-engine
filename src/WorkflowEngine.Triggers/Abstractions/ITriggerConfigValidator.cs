using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Validates trigger configuration.
/// </summary>
public interface ITriggerConfigValidator
{
    /// <summary>
    /// Validates a trigger configuration.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    TriggerValidationResult Validate(TriggerConfig config);
}

/// <summary>
/// Result of trigger configuration validation.
/// </summary>
public sealed class TriggerValidationResult
{
    /// <summary>
    /// Gets whether the configuration is valid.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static TriggerValidationResult Success(IReadOnlyList<string>? warnings = null) => new()
    {
        Warnings = warnings ?? []
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static TriggerValidationResult Failure(IReadOnlyList<string> errors, IReadOnlyList<string>? warnings = null) => new()
    {
        Errors = errors,
        Warnings = warnings ?? []
    };
}
