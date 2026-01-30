using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates that tasks have a run command.
/// </summary>
public sealed class TaskCommandValidationRule : ITaskValidationRule
{
    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid (reported by TaskIdValidationRule)

        if (string.IsNullOrWhiteSpace(task.Run))
        {
            context.AddError("TK004", $"Task '{task.Id}' must have a 'run' command", task.Id);
        }
    }
}
