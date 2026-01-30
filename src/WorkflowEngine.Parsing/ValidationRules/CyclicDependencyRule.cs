using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Parsing.ValidationRules;

/// <summary>
/// Validates that there are no cyclic dependencies between tasks.
/// </summary>
public sealed class CyclicDependencyRule : IValidationRule
{
    /// <inheritdoc />
    public void Validate(Workflow workflow, ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        if (CycleDetector.TryDetectCycle(workflow.Tasks, out var cyclePath))
            context.AddError("TK011", $"Circular dependency detected: {cyclePath}");
    }
}
