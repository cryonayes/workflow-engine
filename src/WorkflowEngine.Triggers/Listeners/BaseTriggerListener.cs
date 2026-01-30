using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Utilities;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Listeners;

/// <summary>
/// Abstract base class for trigger listeners with common functionality.
/// </summary>
public abstract class BaseTriggerListener : ITriggerListener
{
    /// <summary>
    /// Logger for derived classes.
    /// </summary>
    protected readonly ILogger Logger;

    private CancellationTokenSource? _cts;
    private volatile bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the base listener.
    /// </summary>
    protected BaseTriggerListener(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract TriggerSource Source { get; }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public event EventHandler<IncomingMessage>? OnMessageReceived;

    /// <inheritdoc />
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            Logger.LogWarning("{Source} listener already started", Source);
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Logger.LogInformation("Starting {Source} listener", Source);

        await ConnectAsync(_cts.Token);
        _isConnected = true;

        Logger.LogInformation("{Source} listener connected", Source);

        _ = RunMessageLoopAsync(_cts.Token);
    }

    /// <inheritdoc />
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
            return;

        Logger.LogInformation("Stopping {Source} listener", Source);

        try
        {
            await _cts.CancelAsync();
            await DisconnectAsync(cancellationToken);
        }
        finally
        {
            _isConnected = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    /// <inheritdoc />
    public abstract Task SendResponseAsync(
        IncomingMessage message,
        string response,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Establishes connection to the message source.
    /// </summary>
    protected abstract Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the message source.
    /// </summary>
    protected abstract Task DisconnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Receives and processes messages. Called in a loop until cancelled.
    /// </summary>
    protected abstract Task ReceiveMessagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Raises the OnMessageReceived event.
    /// </summary>
    protected void PublishMessage(IncomingMessage message)
    {
        try
        {
            OnMessageReceived?.Invoke(this, message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in message received handler");
        }
    }

    /// <summary>
    /// Sets the connected state.
    /// </summary>
    protected void SetConnected(bool connected) => _isConnected = connected;

    /// <summary>
    /// Gets backoff delay for reconnection attempts.
    /// </summary>
    protected static TimeSpan GetBackoffDelay(int attempt) => BackoffCalculator.CalculateWithJitter(attempt);

    private async Task RunMessageLoopAsync(CancellationToken cancellationToken)
    {
        var consecutiveErrors = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ReceiveMessagesAsync(cancellationToken);
                consecutiveErrors = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                Logger.LogError(ex, "{Source} message loop error (attempt {Attempt})", Source, consecutiveErrors);

                var delay = GetBackoffDelay(consecutiveErrors);
                await Task.Delay(delay, cancellationToken);
            }
        }

        Logger.LogDebug("{Source} message loop stopped", Source);
    }
}
