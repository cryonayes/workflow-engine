using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Executes scheduled workflows manually or via dispatch.
/// </summary>
public interface IScheduleExecutor
{
    /// <summary>
    /// Manually triggers a scheduled workflow.
    /// </summary>
    /// <param name="scheduleId">The schedule ID to trigger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The run ID.</returns>
    Task<string> TriggerScheduleAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually dispatches a workflow with parameters.
    /// </summary>
    /// <param name="request">The dispatch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The run ID.</returns>
    Task<string> DispatchAsync(ManualDispatchRequest request, CancellationToken cancellationToken = default);
}
