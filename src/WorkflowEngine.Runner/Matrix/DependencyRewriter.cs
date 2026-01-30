using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Matrix;

/// <summary>
/// Rewrites task dependencies after matrix expansion.
/// </summary>
public sealed class DependencyRewriter : IDependencyRewriter
{
    private readonly IExpandedTaskBuilder _taskBuilder;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public DependencyRewriter(IExpandedTaskBuilder taskBuilder)
    {
        ArgumentNullException.ThrowIfNull(taskBuilder);
        _taskBuilder = taskBuilder;
    }

    /// <inheritdoc />
    public WorkflowTask Rewrite(WorkflowTask task, IReadOnlyDictionary<string, IReadOnlyList<string>> expansionMap)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(expansionMap);

        if (task.DependsOn.Count == 0)
            return task;

        var newDependencies = new List<string>();

        foreach (var dep in task.DependsOn)
        {
            if (expansionMap.TryGetValue(dep, out var expandedIds))
            {
                // If the dependent task was a matrix task that expanded to multiple tasks,
                // and the current task is also from the same matrix expansion with the same values,
                // only depend on the matching expanded task
                if (task.MatrixValues is not null && expandedIds.Count > 1)
                {
                    // Find if there's a matching expanded task with same matrix values
                    var matchingDep = FindMatchingExpandedDependency(dep, task.MatrixValues, expansionMap);
                    if (matchingDep is not null)
                    {
                        newDependencies.Add(matchingDep);
                        continue;
                    }
                }

                // Otherwise, depend on all expanded tasks
                newDependencies.AddRange(expandedIds);
            }
            else
            {
                // Dependency wasn't expanded, keep as is
                newDependencies.Add(dep);
            }
        }

        if (newDependencies.SequenceEqual(task.DependsOn))
            return task;

        return new WorkflowTask
        {
            Id = task.Id,
            Name = task.Name,
            Run = task.Run,
            Shell = task.Shell,
            WorkingDirectory = task.WorkingDirectory,
            Environment = task.Environment,
            If = task.If,
            Input = task.Input,
            Output = task.Output,
            TimeoutMs = task.TimeoutMs,
            ContinueOnError = task.ContinueOnError,
            RetryCount = task.RetryCount,
            RetryDelayMs = task.RetryDelayMs,
            DependsOn = newDependencies,
            Matrix = task.Matrix,
            MatrixValues = task.MatrixValues
        };
    }

    /// <summary>
    /// Finds an expanded dependency that matches the current task's matrix values.
    /// </summary>
    private string? FindMatchingExpandedDependency(
        string originalDep,
        IReadOnlyDictionary<string, string> currentMatrixValues,
        IReadOnlyDictionary<string, IReadOnlyList<string>> expansionMap)
    {
        if (!expansionMap.TryGetValue(originalDep, out var expandedIds))
            return null;

        // Try to find an expanded ID that matches the current matrix values
        foreach (var expandedId in expandedIds)
        {
            var suffix = GenerateExpectedSuffix(currentMatrixValues);
            if (expandedId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                expandedId.Equals($"{originalDep}-{suffix}", StringComparison.OrdinalIgnoreCase))
            {
                return expandedId;
            }
        }

        return null;
    }

    private string GenerateExpectedSuffix(IReadOnlyDictionary<string, string> matrixValues)
    {
        return string.Join("-", matrixValues.Values.Select(_taskBuilder.SanitizeIdComponent));
    }
}
