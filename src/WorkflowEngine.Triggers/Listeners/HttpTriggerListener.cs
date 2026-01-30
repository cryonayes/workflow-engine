using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Infrastructure;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Listeners;

/// <summary>
/// Generic HTTP webhook listener.
/// </summary>
public sealed class HttpTriggerListener : BaseTriggerListener
{
    private readonly HttpListener _httpListener;
    private readonly HttpServerConfig _config;
    private readonly ISlackEventProcessor? _slackEventProcessor;

    /// <summary>
    /// Initializes a new instance of the HttpTriggerListener.
    /// </summary>
    public HttpTriggerListener(
        HttpServerConfig config,
        ISlackEventProcessor? slackEventProcessor,
        ILogger<HttpTriggerListener> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
        _slackEventProcessor = slackEventProcessor;
        _httpListener = new HttpListener();
    }

    /// <inheritdoc />
    public override TriggerSource Source => TriggerSource.Http;

    /// <inheritdoc />
    protected override Task ConnectAsync(CancellationToken cancellationToken)
    {
        var scheme = _config.EnableHttps ? "https" : "http";
        var url = $"{scheme}://{_config.Host}:{_config.Port}/";

        _httpListener.Prefixes.Add(url);
        _httpListener.Start();

        Logger.LogInformation("HTTP listener started on {Url}", url);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _httpListener.Stop();
        _httpListener.Close();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var context = await _httpListener.GetContextAsync().WaitAsync(cancellationToken);
            _ = HandleRequestAsync(context, cancellationToken);
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 995)
        {
            // Operation aborted - normal shutdown
            throw new OperationCanceledException();
        }
    }

    /// <inheritdoc />
    public override Task SendResponseAsync(
        IncomingMessage message,
        string response,
        CancellationToken cancellationToken = default)
    {
        // HTTP webhooks are fire-and-forget
        Logger.LogDebug("HTTP response: {Response}", response);
        return Task.CompletedTask;
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod != "POST")
            {
                await WriteResponseAsync(response, 405, "Method Not Allowed");
                return;
            }

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync(cancellationToken);
            var path = request.Url?.AbsolutePath?.ToLowerInvariant() ?? "/";

            var result = path switch
            {
                "/slack/events" => HandleSlackEvent(request, body),
                "/webhook" or "/trigger" => HandleGenericWebhook(request, body),
                "/health" => (200, "OK"),
                _ => (404, "Not Found")
            };

            await WriteResponseAsync(response, result.Item1, result.Item2);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling HTTP request");
            await WriteResponseAsync(response, 500, "Internal Server Error");
        }
        finally
        {
            response.Close();
        }
    }

    private (int, string) HandleSlackEvent(HttpListenerRequest request, string body)
    {
        if (_slackEventProcessor is null)
            return (503, "Slack event processor not configured");

        var timestamp = request.Headers["X-Slack-Request-Timestamp"] ?? string.Empty;
        var signature = request.Headers["X-Slack-Signature"] ?? string.Empty;
        var result = _slackEventProcessor.ProcessSlackEvent(body, timestamp, signature);

        return result is null ? (401, "Unauthorized") : (200, result);
    }

    private (int, string) HandleGenericWebhook(HttpListenerRequest request, string body)
    {
        var (text, metadata) = ExtractWebhookPayload(request, body);

        var messageId = Guid.NewGuid().ToString("N");
        var incoming = IncomingMessageFactory.FromHttp(messageId, text, metadata, body);

        Logger.LogDebug("HTTP webhook: {Text}",
            text.Length > 50 ? text[..50] + "..." : text);

        PublishMessage(incoming);

        return (200, JsonSerializer.Serialize(new { success = true, messageId }));
    }

    private static (string Text, Dictionary<string, string> Metadata) ExtractWebhookPayload(
        HttpListenerRequest request,
        string body)
    {
        var metadata = new Dictionary<string, string>();
        string text = body;

        // Try to extract text from JSON
        if (request.ContentType?.Contains("application/json") == true)
        {
            try
            {
                var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                text = TryGetStringProperty(root, "text")
                    ?? TryGetStringProperty(root, "message")
                    ?? TryGetStringProperty(root, "body")
                    ?? body;

                foreach (var prop in root.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.String))
                {
                    metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // Keep body as text
            }
        }

        // Add headers
        foreach (var key in request.Headers.AllKeys.Where(k => k is not null && !k.StartsWith("Content-")))
        {
            metadata[$"header:{key}"] = request.Headers[key!] ?? string.Empty;
        }

        // Add query parameters
        foreach (var key in request.QueryString.AllKeys.Where(k => k is not null))
        {
            metadata[$"query:{key}"] = request.QueryString[key!] ?? string.Empty;
        }

        return (text, metadata);
    }

    private static string? TryGetStringProperty(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static async Task WriteResponseAsync(HttpListenerResponse response, int statusCode, string body)
    {
        response.StatusCode = statusCode;
        response.ContentType = body.StartsWith('{') ? "application/json" : "text/plain";

        var bytes = Encoding.UTF8.GetBytes(body);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }
}
