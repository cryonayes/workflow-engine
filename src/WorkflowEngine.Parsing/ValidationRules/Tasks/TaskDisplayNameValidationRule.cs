using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates task display names and adds warnings for best practices.
/// </summary>
public sealed class TaskDisplayNameValidationRule : ITaskValidationRule
{
    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid

        if (string.IsNullOrEmpty(task.Name))
        {
            context.AddWarning("TK100", $"Task '{task.Id}' has no display name", task.Id);
        }
    }
}
