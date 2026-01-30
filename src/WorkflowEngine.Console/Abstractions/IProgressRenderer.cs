using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Abstractions;

/// <summary>
/// Renders workflow execution progress.
/// </summary>
public interface IProgressRenderer : IDisposable
{
    /// <summary>
    /// Sets the execution plan and workflow for rendering.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="workflow">The workflow definition.</param>
    void SetExecutionPlan(ExecutionPlan plan, Workflow workflow);

    /// <summary>
    /// Handles a workflow event.
    /// </summary>
    void OnWorkflowEvent(object? sender, WorkflowEvent evt);

    /// <summary>
    /// Handles a task event.
    /// </summary>
    void OnTaskEvent(object? sender, TaskEvent evt);

    /// <summary>
    /// Waits for the user to request exit.
    /// </summary>
    Task WaitForExitAsync();
}
