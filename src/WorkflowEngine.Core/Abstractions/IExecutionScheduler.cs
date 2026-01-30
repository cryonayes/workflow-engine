using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Schedules task execution order based on dependencies.
/// </summary>
public interface IExecutionScheduler
{
    /// <summary>
    /// Builds an execution plan for the workflow tasks.
    /// </summary>
    /// <param name="workflow">The workflow to schedule.</param>
    /// <returns>An execution plan with ordered task waves.</returns>
    /// <exception cref="Core.Exceptions.CircularDependencyException">
    /// Thrown when circular dependencies are detected.
    /// </exception>
    ExecutionPlan BuildExecutionPlan(Workflow workflow);
}

/// <summary>
/// Represents a group of tasks that can be executed in parallel.
/// </summary>
public sealed class ExecutionWave
{
    /// <summary>
    /// Gets the zero-based index of this wave.
    /// </summary>
    public int WaveIndex { get; init; }

    /// <summary>
    /// Gets the tasks in this wave.
    /// </summary>
    public IReadOnlyList<WorkflowTask> Tasks { get; init; } = [];

    /// <summary>
    /// Gets the number of tasks in this wave.
    /// </summary>
    public int Count => Tasks.Count;
}

/// <summary>
/// Represents a complete execution plan for a workflow.
/// </summary>
public sealed class ExecutionPlan
{
    /// <summary>
    /// Gets the ordered waves of tasks to execute.
    /// </summary>
    public IReadOnlyList<ExecutionWave> Waves { get; init; } = [];

    /// <summary>
    /// Gets tasks with always() condition that run regardless of other task outcomes.
    /// </summary>
    public IReadOnlyList<WorkflowTask> AlwaysTasks { get; init; } = [];

    /// <summary>
    /// Gets the total number of tasks in the plan.
    /// </summary>
    public int TotalTasks => Waves.Sum(w => w.Count) + AlwaysTasks.Count;

    /// <summary>
    /// Gets the number of execution waves.
    /// </summary>
    public int WaveCount => Waves.Count;
}
