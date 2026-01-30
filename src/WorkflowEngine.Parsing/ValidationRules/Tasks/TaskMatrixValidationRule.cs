using System.Text.RegularExpressions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Validates task matrix configuration.
/// </summary>
public sealed partial class TaskMatrixValidationRule : ITaskValidationRule
{
    [GeneratedRegex(@"\$\{\{\s*matrix\.(\w+)\s*\}\}")]
    private static partial Regex MatrixExpressionPattern();

    /// <inheritdoc />
    public void Validate(WorkflowTask task, int taskIndex, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
            return; // Skip if ID is invalid

        if (task.Matrix is null)
            return;

        var validKeys = ValidateDimensions(task, context);
        ValidateExcludes(task, context, validKeys);
        ValidateMatrixExpressions(task, context, validKeys);
    }

    private static HashSet<string> ValidateDimensions(WorkflowTask task, ValidationContext context)
    {
        var validKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (dimName, dimValues) in task.Matrix!.Dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimName))
            {
                context.AddError("MX001", $"Task '{task.Id}' has a matrix dimension with empty name", task.Id);
                continue;
            }

            validKeys.Add(dimName);

            if (dimValues.Count == 0)
            {
                context.AddError("MX002", $"Task '{task.Id}' matrix dimension '{dimName}' has no values", task.Id);
            }
        }

        return validKeys;
    }

    private static void ValidateExcludes(WorkflowTask task, ValidationContext context, HashSet<string> validKeys)
    {
        foreach (var exclude in task.Matrix!.Exclude)
        {
            foreach (var key in exclude.Keys)
            {
                if (!validKeys.Contains(key))
                {
                    context.AddWarning(
                        "MX100",
                        $"Task '{task.Id}' matrix exclude references unknown dimension '{key}'",
                        task.Id);
                }
            }
        }
    }

    private static void ValidateMatrixExpressions(
        WorkflowTask task,
        ValidationContext context,
        HashSet<string> validKeys)
    {
        if (!task.Matrix!.HasDimensions && task.Matrix.Include.Count == 0)
            return;

        // Collect all valid keys including those from include
        var allValidKeys = new HashSet<string>(validKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var include in task.Matrix.Include)
        {
            foreach (var key in include.Keys)
            {
                allValidKeys.Add(key);
            }
        }

        // Validate task ID
        ValidateMatrixExpressionsInString(task.Id, task.Id, "id", context, allValidKeys);

        // Validate name
        if (task.Name is not null)
            ValidateMatrixExpressionsInString(task.Name, task.Id, "name", context, allValidKeys);

        // Validate run command
        ValidateMatrixExpressionsInString(task.Run, task.Id, "run", context, allValidKeys);

        // Validate environment values
        foreach (var (envKey, envValue) in task.Environment)
        {
            ValidateMatrixExpressionsInString(envValue, task.Id, $"environment.{envKey}", context, allValidKeys);
        }
    }

    private static void ValidateMatrixExpressionsInString(
        string value,
        string taskId,
        string propertyName,
        ValidationContext context,
        HashSet<string> validKeys)
    {
        var matches = MatrixExpressionPattern().Matches(value);
        foreach (Match match in matches)
        {
            var matrixKey = match.Groups[1].Value;
            if (!validKeys.Contains(matrixKey))
            {
                context.AddError(
                    "MX003",
                    $"Task '{taskId}' {propertyName} references unknown matrix key '{matrixKey}'",
                    taskId);
            }
        }
    }
}
