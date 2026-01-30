using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules;

/// <summary>
/// Strategy interface for workflow validation rules.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Validates the workflow and reports any errors or warnings.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <param name="context">The validation context to add errors/warnings to.</param>
    void Validate(Workflow workflow, ValidationContext context);
}

/// <summary>
/// Context for collecting validation results.
/// </summary>
public sealed class ValidationContext
{
    private readonly List<ValidationError> _errors = [];
    private readonly List<ValidationWarning> _warnings = [];
    private readonly HashSet<string> _knownTaskIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the collected errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors;

    /// <summary>
    /// Gets the collected warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings => _warnings;

    /// <summary>
    /// Gets known task IDs for dependency validation.
    /// </summary>
    public IReadOnlySet<string> KnownTaskIds => _knownTaskIds;

    /// <summary>
    /// Adds an error to the validation results.
    /// </summary>
    public void AddError(string code, string message, string? taskId = null) =>
        _errors.Add(new ValidationError(code, message, taskId));

    /// <summary>
    /// Adds a warning to the validation results.
    /// </summary>
    public void AddWarning(string code, string message, string? taskId = null) =>
        _warnings.Add(new ValidationWarning(code, message, taskId));

    /// <summary>
    /// Registers a task ID as known.
    /// </summary>
    /// <returns>False if the task ID already exists (duplicate).</returns>
    public bool RegisterTaskId(string taskId) => _knownTaskIds.Add(taskId);
}
