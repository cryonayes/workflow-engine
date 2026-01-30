using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules;

/// <summary>
/// Validates workflow-level metadata.
/// </summary>
public sealed class WorkflowMetadataRule : IValidationRule
{
    /// <inheritdoc />
    public void Validate(Workflow workflow, ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(workflow.Name))
            context.AddError("WF001", "Workflow name is required");

        if (workflow.DefaultTimeoutMs <= 0)
            context.AddError("WF002", "Default timeout must be positive");

        if (string.IsNullOrEmpty(workflow.Description))
            context.AddWarning("WF100", "Workflow description is recommended");
    }
}
