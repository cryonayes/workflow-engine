using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Webhooks.Providers;

/// <summary>
/// Base class for webhook providers with common HTTP functionality.
/// </summary>
public abstract class BaseWebhookProvider : IWebhookProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public abstract string ProviderType { get; }

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    /// <param name="httpClient">The HTTP client for sending requests.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    protected BaseWebhookProvider(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WebhookResult> SendAsync(
        WebhookConfig config,
        WebhookNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(notification);

        var stopwatch = Stopwatch.StartNew();
        var attempts = 0;
        Exception? lastException = null;
        int? lastStatusCode = null;

        var maxAttempts = config.RetryCount + 1;
        var timeout = TimeSpan.FromMilliseconds(config.TimeoutMs);

        while (attempts < maxAttempts)
        {
            attempts++;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                var payload = BuildPayload(config, notification);
                var request = BuildRequest(config, payload);

                _logger.LogDebug(
                    "Sending webhook ({ProviderType}) attempt {Attempt}/{MaxAttempts} to {Url}",
                    ProviderType, attempts, maxAttempts, config.Url);

                var response = await _httpClient.SendAsync(request, cts.Token);
                lastStatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    stopwatch.Stop();
                    _logger.LogDebug(
                        "Webhook delivered successfully to {Url} (status: {StatusCode}, duration: {Duration}ms)",
                        config.Url, lastStatusCode, stopwatch.ElapsedMilliseconds);

                    return WebhookResult.Success(lastStatusCode.Value, stopwatch.Elapsed, attempts);
                }

                // Check for retryable status codes
                if (!IsRetryableStatusCode(response.StatusCode))
                {
                    stopwatch.Stop();
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var errorMessage = $"HTTP {lastStatusCode}: {errorBody}";

                    _logger.LogWarning(
                        "Webhook failed with non-retryable status {StatusCode}: {Error}",
                        lastStatusCode, errorBody);

                    return WebhookResult.Failed(errorMessage, stopwatch.Elapsed, lastStatusCode, attempts: attempts);
                }

                _logger.LogWarning(
                    "Webhook attempt {Attempt} failed with status {StatusCode}, will retry",
                    attempts, lastStatusCode);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException($"Webhook request timed out after {timeout.TotalMilliseconds}ms");
                _logger.LogWarning("Webhook attempt {Attempt} timed out after {Timeout}ms", attempts, timeout.TotalMilliseconds);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Webhook attempt {Attempt} failed with HTTP error", attempts);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Webhook attempt {Attempt} failed with unexpected error", attempts);
            }

            // Wait before retry with exponential backoff
            if (attempts < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts - 1)); // 1s, 2s, 4s, ...
                await Task.Delay(delay, cancellationToken);
            }
        }

        stopwatch.Stop();
        var finalMessage = lastException?.Message ?? $"All {maxAttempts} attempts failed";

        _logger.LogError(
            "Webhook delivery failed after {Attempts} attempts: {Error}",
            attempts, finalMessage);

        return WebhookResult.Failed(finalMessage, stopwatch.Elapsed, lastStatusCode, lastException, attempts);
    }

    /// <summary>
    /// Builds the request payload for the webhook.
    /// </summary>
    /// <param name="config">The webhook configuration.</param>
    /// <param name="notification">The notification to send.</param>
    /// <returns>The JSON payload object.</returns>
    protected abstract object BuildPayload(WebhookConfig config, WebhookNotification notification);

    /// <summary>
    /// Builds the HTTP request for the webhook.
    /// </summary>
    /// <param name="config">The webhook configuration.</param>
    /// <param name="payload">The payload to send.</param>
    /// <returns>The configured HTTP request message.</returns>
    protected virtual HttpRequestMessage BuildRequest(WebhookConfig config, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, config.Url)
        {
            Content = content
        };

        // Add custom headers
        foreach (var (key, value) in config.Headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }

        return request;
    }

    /// <summary>
    /// Gets the JSON serializer options for payloads.
    /// </summary>
    protected static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Determines whether the status code indicates a retryable error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if the request should be retried.</returns>
    protected virtual bool IsRetryableStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.InternalServerError;

    /// <summary>
    /// Gets the color code for an event type.
    /// </summary>
    protected static int GetColorForEvent(WebhookEventType eventType) =>
        WebhookFormatting.GetEventColor(eventType);

    /// <summary>
    /// Gets the emoji for an event type.
    /// </summary>
    protected static string GetEmojiForEvent(WebhookEventType eventType) =>
        WebhookFormatting.GetEventEmoji(eventType);

    /// <summary>
    /// Gets a human-readable title for an event type.
    /// </summary>
    protected static string GetEventTitle(WebhookEventType eventType) =>
        WebhookFormatting.GetEventTitle(eventType);

    /// <summary>
    /// Formats a duration in a human-readable format.
    /// </summary>
    protected static string FormatDuration(TimeSpan duration) =>
        TextFormatting.FormatDuration(duration);

    /// <summary>
    /// Truncates text to a maximum length.
    /// </summary>
    protected static string TruncateText(string text, int maxLength) =>
        TextFormatting.Truncate(text, maxLength);

    /// <summary>
    /// Formats error text for display.
    /// </summary>
    protected static string FormatError(string error, int maxLength, bool wrapInCodeBlock = false) =>
        WebhookFormatting.FormatError(error, maxLength, wrapInCodeBlock);
}
