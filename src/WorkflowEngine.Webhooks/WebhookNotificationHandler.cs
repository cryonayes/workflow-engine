using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Events;

namespace WorkflowEngine.Webhooks;

/// <summary>
/// Handles webhook notifications by subscribing to workflow/task events and dispatching to providers.
/// Uses a background channel queue for non-blocking dispatch.
/// </summary>
public sealed class WebhookNotificationHandler : IWebhookNotifier
{
    private readonly IReadOnlyDictionary<string, IWebhookProvider> _providers;
    private readonly IExpressionInterpolator _interpolator;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WebhookNotificationHandler> _logger;

    private readonly ConcurrentDictionary<string, WebhookRegistration> _registrations = new();
    private readonly Channel<WebhookDispatchItem> _dispatchQueue;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    public WebhookNotificationHandler(
        IEnumerable<IWebhookProvider> providers,
        IExpressionInterpolator interpolator,
        IEventPublisher eventPublisher,
        ILogger<WebhookNotificationHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(interpolator);
        ArgumentNullException.ThrowIfNull(eventPublisher);
        ArgumentNullException.ThrowIfNull(logger);

        _providers = providers.ToDictionary(p => p.ProviderType, StringComparer.OrdinalIgnoreCase);
        _interpolator = interpolator;
        _eventPublisher = eventPublisher;
        _logger = logger;

        _dispatchQueue = Channel.CreateUnbounded<WebhookDispatchItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _processingTask = ProcessQueueAsync(_cts.Token);

        _eventPublisher.OnWorkflowEvent += OnWorkflowEvent;
        _eventPublisher.OnTaskEvent += OnTaskEvent;

        _logger.LogInformation(
            "WebhookNotificationHandler initialized with {ProviderCount} providers: {Providers}",
            _providers.Count, string.Join(", ", _providers.Keys));
    }

    /// <inheritdoc />
    public void RegisterWebhooks(string runId, string workflowName, IReadOnlyList<WebhookConfig> configs)
    {
        ArgumentNullException.ThrowIfNull(runId);
        ArgumentNullException.ThrowIfNull(workflowName);
        ArgumentNullException.ThrowIfNull(configs);

        if (configs.Count == 0)
            return;

        _registrations[runId] = new WebhookRegistration(workflowName, configs);
        _logger.LogDebug("Registered {Count} webhooks for run {RunId}", configs.Count, runId);
    }

    /// <inheritdoc />
    public void UnregisterWebhooks(string runId)
    {
        if (_registrations.TryRemove(runId, out _))
        {
            _logger.LogDebug("Unregistered webhooks for run {RunId}", runId);
        }
    }

    private void OnWorkflowEvent(object? sender, WorkflowEvent evt)
    {
        var eventType = WebhookEventMapper.MapWorkflowEvent(evt);
        if (eventType is null || !_registrations.TryGetValue(evt.RunId, out var registration))
            return;

        var notification = WebhookNotificationFactory.Create(evt, eventType.Value, registration.WorkflowName);
        EnqueueNotifications(registration.Configs, notification);
    }

    private void OnTaskEvent(object? sender, TaskEvent evt)
    {
        var eventType = WebhookEventMapper.MapTaskEvent(evt);
        if (eventType is null || !_registrations.TryGetValue(evt.RunId, out var registration))
            return;

        var notification = WebhookNotificationFactory.Create(evt, eventType.Value, registration.WorkflowName);
        EnqueueNotifications(registration.Configs, notification);
    }

    private void EnqueueNotifications(IReadOnlyList<WebhookConfig> configs, WebhookNotification notification)
    {
        foreach (var config in configs)
        {
            if (!config.Events.Contains(notification.EventType))
                continue;

            if (!_providers.TryGetValue(config.Provider, out var provider))
            {
                _logger.LogWarning("No provider found for webhook type '{Provider}'", config.Provider);
                continue;
            }

            if (!_dispatchQueue.Writer.TryWrite(new WebhookDispatchItem(config, notification, provider)))
            {
                _logger.LogWarning("Failed to enqueue webhook notification for {EventType}", notification.EventType);
            }
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Webhook dispatch processor started");

        try
        {
            await foreach (var item in _dispatchQueue.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessItemAsync(item, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Webhook dispatch processor stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook dispatch processor encountered fatal error");
        }

        _logger.LogDebug("Webhook dispatch processor stopped");
    }

    private async Task ProcessItemAsync(WebhookDispatchItem item, CancellationToken cancellationToken)
    {
        try
        {
            var config = InterpolateConfig(item.Config, item.Notification);
            var result = await item.Provider.SendAsync(config, item.Notification, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogDebug(
                    "Webhook delivered: {Provider} -> {EventType} ({Duration}ms)",
                    item.Config.Provider, item.Notification.EventType, result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook delivery failed: {Provider} -> {EventType}: {Error}",
                    item.Config.Provider, item.Notification.EventType, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing webhook for {Provider}: {EventType}",
                item.Config.Provider, item.Notification.EventType);
        }
    }

    private WebhookConfig InterpolateConfig(WebhookConfig config, WebhookNotification notification)
    {
        var context = CreateInterpolationContext(notification.WorkflowName);

        return new WebhookConfig
        {
            Provider = config.Provider,
            Url = _interpolator.Interpolate(config.Url, context),
            Name = config.Name,
            Events = config.Events,
            Headers = InterpolateHeaders(config.Headers, context),
            Options = config.Options,
            TimeoutMs = config.TimeoutMs,
            RetryCount = config.RetryCount
        };
    }

    private WorkflowContext CreateInterpolationContext(string workflowName) => new()
    {
        Workflow = new Workflow { Name = workflowName, Tasks = [] },
        CancellationToken = CancellationToken.None
    };

    private IReadOnlyDictionary<string, string> InterpolateHeaders(
        IReadOnlyDictionary<string, string> headers,
        WorkflowContext context)
    {
        if (headers.Count == 0)
            return headers;

        return headers.ToDictionary(
            kvp => kvp.Key,
            kvp => _interpolator.Interpolate(kvp.Value, context));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _eventPublisher.OnWorkflowEvent -= OnWorkflowEvent;
        _eventPublisher.OnTaskEvent -= OnTaskEvent;

        await _cts.CancelAsync();
        _dispatchQueue.Writer.Complete();

        try
        {
            await _processingTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Webhook processor did not stop gracefully within timeout");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        _cts.Dispose();
        _registrations.Clear();

        _logger.LogDebug("WebhookNotificationHandler disposed");
    }

    private sealed record WebhookRegistration(string WorkflowName, IReadOnlyList<WebhookConfig> Configs);

    private sealed record WebhookDispatchItem(
        WebhookConfig Config,
        WebhookNotification Notification,
        IWebhookProvider Provider);
}
