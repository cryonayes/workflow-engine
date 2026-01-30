using System.Text.RegularExpressions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates that expression references in tasks point to known tasks.
/// </summary>
public sealed partial class TaskExpressionValidationRule : ITaskValidationRule
{
    [GeneratedRegex(@"\$\{\{\s*tasks\.(\w+)\.")]
    private static partial Regex TaskReferencePattern();

    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid

        if (task.If is not null)
            ValidateExpression(task.If, context, task.Id, "condition");

        if (task.Input?.Value is not null)
            ValidateExpression(task.Input.Value, context, task.Id, "input");
    }

    private static void ValidateExpression(
        string expression,
        ValidationContext context,
        string currentTaskId,
        string expressionType)
    {
        var matches = TaskReferencePattern().Matches(expression);

        foreach (Match match in matches)
        {
            var referencedTaskId = match.Groups[1].Value;
            if (!context.KnownTaskIds.Contains(referencedTaskId))
            {
                context.AddError(
                    "TK010",
                    $"Task '{currentTaskId}' {expressionType} references unknown task '{referencedTaskId}'",
                    currentTaskId);
            }
        }
    }
}
