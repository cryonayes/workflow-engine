using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Runner;

/// <summary>
/// Builds an execution plan using DAG scheduling. Tasks are grouped into waves
/// where tasks in the same wave can run in parallel.
/// </summary>
public sealed class DagScheduler : IExecutionScheduler
{
    private readonly IMatrixExpander _matrixExpander;

    /// <summary>
    /// Initializes a new instance with the required matrix expander.
    /// </summary>
    /// <param name="matrixExpander">The matrix expander for expanding matrix-configured tasks.</param>
    public DagScheduler(IMatrixExpander matrixExpander)
    {
        ArgumentNullException.ThrowIfNull(matrixExpander);
        _matrixExpander = matrixExpander;
    }

    /// <inheritdoc />
    public ExecutionPlan BuildExecutionPlan(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        if (workflow.Tasks.Count == 0)
        {
            return new ExecutionPlan();
        }

        // Early cycle detection on original tasks (before expensive matrix expansion)
        // This catches most cycles with minimal overhead
        var originalTasks = workflow.Tasks.Where(t => !IsAlwaysTask(t)).ToList();
        if (CycleDetector.TryDetectCycle(originalTasks, out var cyclePath))
        {
            throw new CircularDependencyException(cyclePath!);
        }

        // Expand matrix tasks after cycle check passes
        var tasks = _matrixExpander.ExpandAll(workflow.Tasks).ToList();

        if (tasks.Count == 0)
        {
            return new ExecutionPlan();
        }

        var taskMap = tasks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        // Separate always() tasks - they run last regardless of dependencies
        var alwaysTasks = tasks
            .Where(IsAlwaysTask)
            .ToList();

        var regularTasks = tasks
            .Except(alwaysTasks)
            .ToList();

        // Build waves using topological sort with level assignment
        var waves = BuildWaves(regularTasks, taskMap);

        return new ExecutionPlan
        {
            Waves = waves,
            AlwaysTasks = alwaysTasks
        };
    }

    private static bool IsAlwaysTask(WorkflowTask task)
    {
        return task.If?.Contains("always()", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static List<ExecutionWave> BuildWaves(
        List<WorkflowTask> tasks,
        Dictionary<string, WorkflowTask> taskMap)
    {
        if (tasks.Count == 0)
            return [];

        // Calculate the "level" of each task based on its dependencies
        // Level 0 = no dependencies, Level N = max(dependency levels) + 1
        var levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var calculating = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var task in tasks)
        {
            CalculateLevel(task.Id, taskMap, levels, calculating);
        }

        // Group tasks by level
        var tasksByLevel = tasks
            .GroupBy(t => levels.GetValueOrDefault(t.Id, 0))
            .OrderBy(g => g.Key)
            .Select((group, index) => new ExecutionWave
            {
                WaveIndex = index,
                Tasks = group.ToList()
            })
            .ToList();

        return tasksByLevel;
    }

    private static int CalculateLevel(
        string taskId,
        Dictionary<string, WorkflowTask> taskMap,
        Dictionary<string, int> levels,
        HashSet<string> calculating)
    {
        if (levels.TryGetValue(taskId, out var existingLevel))
            return existingLevel;

        if (!taskMap.TryGetValue(taskId, out var task))
            return 0;

        // Guard against cycles (shouldn't happen if cycle detection runs first)
        if (calculating.Contains(taskId))
            return 0;

        calculating.Add(taskId);

        var maxDepLevel = -1;
        foreach (var dep in task.DependsOn)
        {
            var depLevel = CalculateLevel(dep, taskMap, levels, calculating);
            maxDepLevel = Math.Max(maxDepLevel, depLevel);
        }

        calculating.Remove(taskId);

        var level = maxDepLevel + 1;
        levels[taskId] = level;
        return level;
    }

}
