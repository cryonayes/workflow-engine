using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates task ID presence and uniqueness.
/// </summary>
public sealed class TaskIdValidationRule : ITaskValidationRule
{
    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
        {
            context.AddError("TK002", $"Task #{taskIndex} must have an ID");
            return;
        }

        if (!context.RegisterTaskId(task.Id))
        {
            context.AddError("TK003", $"Duplicate task ID: '{task.Id}'", task.Id);
        }
    }
}
