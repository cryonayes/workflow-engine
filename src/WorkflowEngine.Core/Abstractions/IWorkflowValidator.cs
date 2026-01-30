using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Validates workflow definitions.
/// </summary>
public interface IWorkflowValidator
{
    /// <summary>
    /// Validates a workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    ValidationResult Validate(Workflow workflow);
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed without errors.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors found.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Gets the list of validation warnings found.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { Errors = [], Warnings = [] };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    public static ValidationResult Failed(params ValidationError[] errors) =>
        new() { Errors = errors };
}

/// <summary>
/// Represents a validation warning.
/// </summary>
/// <param name="Code">Warning code for programmatic handling.</param>
/// <param name="Message">Human-readable warning message.</param>
/// <param name="TaskId">Optional task ID where the warning occurred.</param>
public sealed record ValidationWarning(string Code, string Message, string? TaskId = null);
