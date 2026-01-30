using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Execution;

/// <summary>
/// Executes waves of tasks with parallel or step-mode execution.
/// </summary>
public interface IWaveExecutor
{
    /// <summary>
    /// Executes a wave of tasks using the provided execution context.
    /// Automatically handles parallel vs step mode based on context.
    /// </summary>
    /// <param name="executionContext">The wave execution context containing all parameters.</param>
    /// <returns>List of task results for the wave.</returns>
    Task<List<TaskResult>> ExecuteWaveAsync(WaveExecutionContext executionContext);
}
