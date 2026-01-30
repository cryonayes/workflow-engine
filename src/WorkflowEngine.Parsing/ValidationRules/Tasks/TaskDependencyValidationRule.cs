using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates that task dependencies reference known tasks.
/// </summary>
public sealed class TaskDependencyValidationRule : ITaskValidationRule
{
    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid

        foreach (var dep in task.DependsOn)
        {
            if (!context.KnownTaskIds.Contains(dep))
            {
                context.AddError(
                    "TK006",
                    $"Task '{task.Id}' depends on unknown task '{dep}'. Dependencies must be declared before the task.",
                    task.Id);
            }
        }
    }
}
