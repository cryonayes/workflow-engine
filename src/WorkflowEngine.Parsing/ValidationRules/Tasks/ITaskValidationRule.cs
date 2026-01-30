using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Interface for individual task validation rules.
/// </summary>
public interface ITaskValidationRule
{
    /// <summary>
    /// Validates a single task.
    /// </summary>
    /// <param name="task">The task to validate.</param>
    /// <param name="taskIndex">The 1-based index of the task in the workflow.</param>
    /// <param name="context">The validation context.</param>
    void Validate(WorkflowTask task, int taskIndex, ValidationContext context);
}
