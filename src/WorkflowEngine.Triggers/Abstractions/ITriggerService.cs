using WorkflowEngine.Triggers.Events;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Main trigger service interface for orchestrating trigger listeners and workflow dispatch.
/// </summary>
public interface ITriggerService : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the trigger service is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the trigger service and all configured listeners.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the trigger service and all listeners gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when trigger events occur.
    /// </summary>
    event EventHandler<TriggerEvent>? OnTriggerEvent;
}
