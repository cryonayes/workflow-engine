using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Interface for dispatching matched triggers to workflow execution.
/// </summary>
public interface ITriggerDispatcher
{
    /// <summary>
    /// Dispatches a matched trigger for workflow execution.
    /// </summary>
    /// <param name="matchResult">The trigger match result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The run ID of the dispatched workflow.</returns>
    Task<string> DispatchAsync(TriggerMatchResult matchResult, CancellationToken cancellationToken = default);
}
