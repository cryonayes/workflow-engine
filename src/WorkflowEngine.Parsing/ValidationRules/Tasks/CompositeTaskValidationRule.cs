using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.ValidationRules.Tasks;

/// <summary>
/// Composite validation rule that runs all task validation rules.
/// Replaces the monolithic TaskDefinitionRule.
/// </summary>
public sealed class CompositeTaskValidationRule : IValidationRule
{
    private readonly IReadOnlyList<ITaskValidationRule> _rules;

    /// <summary>
    /// Creates a new composite rule with default task validation rules.
    /// </summary>
    public CompositeTaskValidationRule()
        : this(DefaultRules)
    {
    }

    /// <summary>
    /// Creates a new composite rule with custom task validation rules.
    /// </summary>
    /// <param name="rules">The task validation rules to use.</param>
    public CompositeTaskValidationRule(IReadOnlyList<ITaskValidationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
    }

    /// <summary>
    /// Gets the default set of task validation rules.
    /// </summary>
    public static IReadOnlyList<ITaskValidationRule> DefaultRules { get; } =
    [
        new TaskIdValidationRule(),
        new TaskCommandValidationRule(),
        new TaskShellValidationRule(),
        new TaskDependencyValidationRule(),
        new TaskExpressionValidationRule(),
        new TaskTimeoutRetryValidationRule(),
        new TaskDisplayNameValidationRule(),
        new TaskMatrixValidationRule()
    ];

    /// <inheritdoc />
    public void Validate(Workflow workflow, ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        var taskIndex = 0;
        foreach (var task in workflow.Tasks)
        {
            taskIndex++;
            foreach (var rule in _rules)
            {
                rule.Validate(task, taskIndex, context);
            }
        }
    }
}
