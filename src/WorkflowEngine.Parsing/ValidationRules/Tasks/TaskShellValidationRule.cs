using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates that task shell configurations are valid.
/// </summary>
public sealed class TaskShellValidationRule : ITaskValidationRule
{
    private static readonly string[] ValidShells = ["bash", "sh", "zsh", "pwsh", "powershell", "cmd"];

    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid

        // Null is valid - means use platform default
        if (task.Shell is not null && !ValidShells.Contains(task.Shell.ToLowerInvariant()))
        {
            context.AddError(
                "TK005",
                $"Task '{task.Id}' has invalid shell '{task.Shell}'. Valid shells: {string.Join(", ", ValidShells)}",
                task.Id);
        }
    }
}
