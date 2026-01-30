using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Utilities;

/// <summary>
/// Detects cycles in task dependency graphs.
/// </summary>
public static class CycleDetector
{
    /// <summary>
    /// Checks for circular dependencies in a list of tasks.
    /// </summary>
    /// <param name="tasks">The tasks to check.</param>
    /// <param name="cyclePath">If a cycle is found, contains the path as "A -> B -> C -> A".</param>
    /// <returns>True if a cycle was detected, false otherwise.</returns>
    public static bool TryDetectCycle(IReadOnlyList<WorkflowTask> tasks, out string? cyclePath)
    {
        cyclePath = null;

        var taskMap = tasks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new Stack<string>();

        foreach (var task in tasks)
        {
            if (HasCycle(task.Id, taskMap, visited, inStack, path))
            {
                cyclePath = string.Join(" -> ", path.Reverse());
                return true;
            }
        }

        return false;
    }

    private static bool HasCycle(
        string taskId,
        Dictionary<string, WorkflowTask> taskMap,
        HashSet<string> visited,
        HashSet<string> inStack,
        Stack<string> path)
    {
        if (inStack.Contains(taskId))
        {
            path.Push(taskId);
            return true;
        }

        if (visited.Contains(taskId))
            return false;

        visited.Add(taskId);
        inStack.Add(taskId);
        path.Push(taskId);

        if (taskMap.TryGetValue(taskId, out var task))
        {
            foreach (var dep in task.DependsOn)
            {
                if (HasCycle(dep, taskMap, visited, inStack, path))
                    return true;
            }
        }

        path.Pop();
        inStack.Remove(taskId);
        return false;
    }
}
