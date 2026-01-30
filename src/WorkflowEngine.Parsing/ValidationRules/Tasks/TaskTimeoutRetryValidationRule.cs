using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates task timeout and retry configurations.
/// </summary>
public sealed class TaskTimeoutRetryValidationRule : ITaskValidationRule
{
    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid

        if (task.RetryCount < 0)
        {
            context.AddError("TK007", $"Task '{task.Id}' has invalid retry count: {task.RetryCount}", task.Id);
        }

        if (task.RetryDelayMs < 0)
        {
            context.AddError("TK008", $"Task '{task.Id}' has invalid retry delay: {task.RetryDelayMs}", task.Id);
        }

        if (task.TimeoutMs.HasValue && task.TimeoutMs.Value <= 0)
        {
            context.AddError("TK009", $"Task '{task.Id}' has invalid timeout: {task.TimeoutMs}", task.Id);
        }
    }
}
