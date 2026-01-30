using WorkflowEngine.Core.Models;
using WorkflowEngine.Parsing.ValidationRules.Tasks;

namespace WorkflowEngine.Parsing.ValidationRules;

/// <summary>
/// Validates individual task definitions.
/// Delegates to CompositeTaskValidationRule for modular validation.
/// </summary>
public sealed class TaskDefinitionRule : IValidationRule
{
    private readonly CompositeTaskValidationRule _compositeRule = new();

    /// <inheritdoc />
    public void Validate(Workflow workflow, ValidationContext context)
    {
        _compositeRule.Validate(workflow, context);
    }
}
