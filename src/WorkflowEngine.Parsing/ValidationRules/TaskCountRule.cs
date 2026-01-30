using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules;

/// <summary>
/// Validates that the workflow has at least one task.
/// </summary>
public sealed class TaskCountRule : IValidationRule
{
    /// <inheritdoc />
    public void Validate(Workflow workflow, ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        if (workflow.Tasks.Count == 0)
            context.AddError("TK001", "Workflow must have at least one task");
    }
}
