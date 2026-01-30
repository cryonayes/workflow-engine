using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Expands matrix-configured tasks into multiple concrete task instances.
/// </summary>
public interface IMatrixExpander
{
    /// <summary>
    /// Expands a single task into multiple tasks based on its matrix configuration.
    /// </summary>
    /// <param name="task">The task to expand.</param>
    /// <returns>
    /// A list of expanded tasks. If the task has no matrix configuration,
    /// returns a single-element list containing the original task.
    /// </returns>
    IReadOnlyList<WorkflowTask> Expand(WorkflowTask task);

    /// <summary>
    /// Expands all tasks in a collection, handling matrix configurations and
    /// updating dependency references to point to expanded task IDs.
    /// </summary>
    /// <param name="tasks">The tasks to expand.</param>
    /// <returns>A list of all expanded tasks with updated dependencies.</returns>
    IReadOnlyList<WorkflowTask> ExpandAll(IEnumerable<WorkflowTask> tasks);
}
