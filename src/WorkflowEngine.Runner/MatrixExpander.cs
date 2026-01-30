using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner;

/// <summary>
/// Expands matrix-configured tasks into multiple concrete task instances.
/// </summary>
public sealed class MatrixExpander : IMatrixExpander
{
    private readonly IMatrixCombinationGenerator _combinationGenerator;
    private readonly IExpandedTaskBuilder _taskBuilder;
    private readonly IDependencyRewriter _dependencyRewriter;

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    public MatrixExpander(
        IMatrixCombinationGenerator combinationGenerator,
        IExpandedTaskBuilder taskBuilder,
        IDependencyRewriter dependencyRewriter)
    {
        ArgumentNullException.ThrowIfNull(combinationGenerator);
        ArgumentNullException.ThrowIfNull(taskBuilder);
        ArgumentNullException.ThrowIfNull(dependencyRewriter);

        _combinationGenerator = combinationGenerator;
        _taskBuilder = taskBuilder;
        _dependencyRewriter = dependencyRewriter;
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkflowTask> Expand(WorkflowTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.Matrix is null || !task.Matrix.HasDimensions && task.Matrix.Include.Count == 0)
        {
            return [task];
        }

        var combinations = _combinationGenerator.Generate(task.Matrix);
        return combinations.Select(combo => _taskBuilder.Build(task, combo)).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkflowTask> ExpandAll(IEnumerable<WorkflowTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var taskList = tasks.ToList();

        // Track original task IDs to their expanded task IDs
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        // First pass: expand all tasks and build the expansion map
        var expandedTasks = new List<WorkflowTask>();
        foreach (var task in taskList)
        {
            var expanded = Expand(task);
            expansionMap[task.Id] = expanded.Select(t => t.Id).ToList();
            expandedTasks.AddRange(expanded);
        }

        // Second pass: rewrite dependencies
        return expandedTasks.Select(task => _dependencyRewriter.Rewrite(task, expansionMap)).ToList();
    }
}
