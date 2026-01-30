using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Parsing.ValidationRules;

namespace WorkflowEngine.Parsing;

/// <summary>
/// Validates workflow definitions for correctness and consistency using strategy pattern.
/// </summary>
public sealed class WorkflowValidator : IWorkflowValidator
{
    private readonly IReadOnlyList<IValidationRule> _rules;

    /// <summary>
    /// Initializes a new instance with default validation rules.
    /// </summary>
    public WorkflowValidator() : this(CreateDefaultRules())
    {
    }

    /// <summary>
    /// Initializes a new instance with custom validation rules.
    /// </summary>
    /// <param name="rules">The validation rules to apply.</param>
    public WorkflowValidator(IEnumerable<IValidationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    private static IEnumerable<IValidationRule> CreateDefaultRules()
    {
        return
        [
            new WorkflowMetadataRule(),
            new TaskCountRule(),
            new TaskDefinitionRule(),
            new CyclicDependencyRule()
        ];
    }

    /// <inheritdoc />
    public ValidationResult Validate(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var context = new ValidationRules.ValidationContext();

        foreach (var rule in _rules)
        {
            rule.Validate(workflow, context);
        }

        return new ValidationResult
        {
            Errors = context.Errors.ToList(),
            Warnings = context.Warnings.ToList()
        };
    }
}
