using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Rewrites task dependencies after matrix expansion.
/// </summary>
public interface IDependencyRewriter
{
    /// <summary>
    /// Rewrites task dependencies to reference expanded task IDs.
    /// </summary>
    /// <param name="task">The task whose dependencies should be rewritten.</param>
    /// <param name="expansionMap">Map of original task IDs to their expanded task IDs.</param>
    /// <returns>A new task with rewritten dependencies, or the original task if no changes needed.</returns>
    WorkflowTask Rewrite(WorkflowTask task, IReadOnlyDictionary<string, IReadOnlyList<string>> expansionMap);
}
