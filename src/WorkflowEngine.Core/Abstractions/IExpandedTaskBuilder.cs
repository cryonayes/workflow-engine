using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Creates expanded task instances from a template and matrix values.
/// </summary>
public interface IExpandedTaskBuilder
{
    /// <summary>
    /// Builds an expanded task from a template and matrix values.
    /// </summary>
    /// <param name="template">The original task template.</param>
    /// <param name="matrixValues">The matrix values for this expansion.</param>
    /// <returns>A new task with interpolated values.</returns>
    WorkflowTask Build(WorkflowTask template, Dictionary<string, string> matrixValues);

    /// <summary>
    /// Generates a unique task ID for an expanded task.
    /// </summary>
    /// <param name="baseId">The original task ID.</param>
    /// <param name="matrixValues">The matrix values used for this expansion.</param>
    /// <returns>A unique task ID.</returns>
    string GenerateTaskId(string baseId, IReadOnlyDictionary<string, string> matrixValues);

    /// <summary>
    /// Sanitizes a value for use in a task ID.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>A sanitized string safe for use in IDs.</returns>
    string SanitizeIdComponent(string value);
}
