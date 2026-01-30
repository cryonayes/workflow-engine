using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Utilities;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Events;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers;

/// <summary>
/// Main trigger service that orchestrates listeners, matching, and dispatch.
/// </summary>
public sealed class TriggerService : ITriggerService
{
    private readonly IReadOnlyList<ITriggerListener> _listeners;
    private readonly ITriggerMatcher _matcher;
    private readonly ITriggerDispatcher _dispatcher;
    private readonly ITemplateResolver _templateResolver;
    private readonly TriggerConfig _config;
    private readonly ILogger<TriggerService> _logger;

    private readonly Channel<IncomingMessage> _messageQueue;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;
    private volatile bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the TriggerService.
    /// </summary>
    public TriggerService(
        IEnumerable<ITriggerListener> listeners,
        ITriggerMatcher matcher,
        ITriggerDispatcher dispatcher,
        ITemplateResolver templateResolver,
        TriggerConfig config,
        ILogger<TriggerService> logger)
    {
        ArgumentNullException.ThrowIfNull(listeners);
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(templateResolver);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _listeners = listeners.ToList();
        _matcher = matcher;
        _dispatcher = dispatcher;
        _templateResolver = templateResolver;
        _config = config;
        _logger = logger;

        _messageQueue = Channel.CreateBounded<IncomingMessage>(new BoundedChannelOptions(TriggerConstants.DefaultMessageQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<TriggerEvent>? OnTriggerEvent;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Trigger service is already running");
            return;
        }

        _logger.LogInformation("Starting trigger service with {ListenerCount} listeners and {RuleCount} rules",
            _listeners.Count, _config.Triggers.Count);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        SubscribeToListeners();
        await StartListenersAsync(_cts.Token);

        _processingTask = ProcessMessagesAsync(_cts.Token);
        _isRunning = true;

        PublishEvent(new TriggerServiceStartedEvent(_listeners.Count, _config.Triggers.Count));
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        _logger.LogInformation("Stopping trigger service");

        if (_cts is not null)
            await _cts.CancelAsync();

        UnsubscribeFromListeners();
        await StopListenersAsync(cancellationToken);
        await WaitForProcessingAsync(cancellationToken);

        _cts?.Dispose();
        _cts = null;
        _isRunning = false;

        PublishEvent(new TriggerServiceStoppedEvent());
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        foreach (var listener in _listeners)
        {
            await listener.DisposeAsync();
        }
    }

    private void SubscribeToListeners()
    {
        foreach (var listener in _listeners)
        {
            listener.OnMessageReceived += OnMessageReceived;
        }
    }

    private void UnsubscribeFromListeners()
    {
        foreach (var listener in _listeners)
        {
            listener.OnMessageReceived -= OnMessageReceived;
        }
    }

    private async Task StartListenersAsync(CancellationToken cancellationToken)
    {
        var tasks = _listeners.Select(listener => StartListenerAsync(listener, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task StartListenerAsync(ITriggerListener listener, CancellationToken cancellationToken)
    {
        try
        {
            await listener.StartAsync(cancellationToken);
            PublishEvent(new ListenerConnectedEvent(listener.Source));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start {Source} listener", listener.Source);
            PublishEvent(new TriggerErrorEvent(listener.Source.ToString(), $"Failed to start: {ex.Message}", ex));
        }
    }

    private async Task StopListenersAsync(CancellationToken cancellationToken)
    {
        var tasks = _listeners.Select(listener => StopListenerAsync(listener, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task StopListenerAsync(ITriggerListener listener, CancellationToken cancellationToken)
    {
        try
        {
            await listener.StopAsync(cancellationToken);
            PublishEvent(new ListenerDisconnectedEvent(listener.Source, "Service stopped"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping {Source} listener", listener.Source);
        }
    }

    private async Task WaitForProcessingAsync(CancellationToken cancellationToken)
    {
        if (_processingTask is null) return;

        try
        {
            await _processingTask.WaitAsync(TriggerConstants.ProcessingShutdownTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Message processing did not complete in time");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    private void OnMessageReceived(object? sender, IncomingMessage message)
    {
        if (!_messageQueue.Writer.TryWrite(message))
        {
            _logger.LogWarning("Message queue full, dropping message from {Source}", message.Source);
            return;
        }

        PublishEvent(new MessageReceivedEvent(
            message.Source,
            message.MessageId,
            TruncateText(message.Text, 100),
            message.Username));
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Message processing started");

        await foreach (var message in _messageQueue.Reader.ReadAllAsync(cancellationToken))
        {
            await ProcessMessageSafelyAsync(message, cancellationToken);
        }

        _logger.LogDebug("Message processing stopped");
    }

    private async Task ProcessMessageSafelyAsync(IncomingMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessMessageAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {Source}", message.Source);
            PublishEvent(new TriggerErrorEvent("MessageProcessor", ex.Message, ex));
        }
    }

    private async Task ProcessMessageAsync(IncomingMessage message, CancellationToken cancellationToken)
    {
        var matchResult = _matcher.Match(message, _config.Triggers);

        if (matchResult is not { IsMatch: true, Rule: not null })
        {
            _logger.LogDebug("No matching rule for message: {Text}", TruncateText(message.Text, 50));
            return;
        }

        var rule = matchResult.Rule;

        PublishEvent(new TriggerMatchedEvent(rule.Name, message.Source, message.MessageId, matchResult.Captures));

        await DispatchAndRespondAsync(matchResult, cancellationToken);
    }

    private async Task DispatchAndRespondAsync(TriggerMatchResult matchResult, CancellationToken cancellationToken)
    {
        var rule = matchResult.Rule!;
        var message = matchResult.Message!;

        try
        {
            var runId = await _dispatcher.DispatchAsync(matchResult, cancellationToken);

            PublishEvent(new TriggerDispatchedEvent(rule.Name, message.Source, rule.WorkflowPath, runId, message.Username));

            await SendResponseIfConfiguredAsync(matchResult, runId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch workflow for rule '{RuleName}'", rule.Name);

            PublishEvent(new TriggerDispatchFailedEvent(rule.Name, message.Source, rule.WorkflowPath, ex.Message));

            await SendErrorResponseAsync(message, ex.Message, cancellationToken);
        }
    }

    private async Task SendResponseIfConfiguredAsync(
        TriggerMatchResult matchResult,
        string runId,
        CancellationToken cancellationToken)
    {
        var rule = matchResult.Rule!;
        var message = matchResult.Message!;

        if (string.IsNullOrEmpty(rule.ResponseTemplate))
            return;

        var response = _templateResolver.Resolve(
            rule.ResponseTemplate,
            matchResult.Captures,
            message,
            new Dictionary<string, string> { ["runId"] = runId });

        await SendResponseAsync(message, response, cancellationToken);
    }

    private async Task SendErrorResponseAsync(IncomingMessage message, string error, CancellationToken cancellationToken)
    {
        await SendResponseAsync(message, $"Failed to trigger workflow: {error}", cancellationToken);
    }

    private async Task SendResponseAsync(IncomingMessage message, string response, CancellationToken cancellationToken)
    {
        var listener = _listeners.FirstOrDefault(l => l.Source == message.Source);
        if (listener is null)
        {
            _logger.LogWarning("No listener found for source {Source}", message.Source);
            return;
        }

        try
        {
            await listener.SendResponseAsync(message, response, cancellationToken);
            PublishEvent(new ResponseSentEvent(message.Source, message.MessageId, response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send response to {Source}", message.Source);
        }
    }

    private void PublishEvent(TriggerEvent evt)
    {
        try
        {
            OnTriggerEvent?.Invoke(this, evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing trigger event");
        }
    }

    private static string TruncateText(string text, int maxLength) =>
        TextFormatting.TruncateSafe(text, maxLength);
}
