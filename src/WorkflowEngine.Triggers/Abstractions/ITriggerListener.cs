using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Interface for platform-specific trigger listeners.
/// </summary>
public interface ITriggerListener : IAsyncDisposable
{
    /// <summary>
    /// Gets the source platform this listener handles.
    /// </summary>
    TriggerSource Source { get; }

    /// <summary>
    /// Gets whether the listener is currently connected and receiving messages.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Starts listening for incoming messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening for messages gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a response message to the source platform.
    /// </summary>
    /// <param name="message">The original incoming message to reply to.</param>
    /// <param name="response">The response text to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendResponseAsync(IncomingMessage message, string response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a message is received from the source platform.
    /// </summary>
    event EventHandler<IncomingMessage>? OnMessageReceived;
}
