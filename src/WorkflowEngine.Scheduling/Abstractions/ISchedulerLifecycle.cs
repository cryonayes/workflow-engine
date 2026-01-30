namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Manages the scheduler service lifecycle (start, stop, status).
/// </summary>
public interface ISchedulerLifecycle
{
    /// <summary>
    /// Gets whether the scheduler is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the scheduler background service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the scheduler background service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
