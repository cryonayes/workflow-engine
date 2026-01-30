using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Infrastructure;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Listeners;

/// <summary>
/// Slack listener using Events API with signature verification.
/// </summary>
public sealed class SlackTriggerListener : BaseTriggerListener, ISlackEventProcessor
{
    private const string ApiBaseUrl = "https://slack.com/api";
    private const int TimestampWindowSeconds = 300;

    private readonly HttpClient _httpClient;
    private readonly string _appToken;
    private readonly string _signingSecret;

    /// <summary>
    /// Initializes a new instance of the SlackTriggerListener.
    /// </summary>
    public SlackTriggerListener(
        HttpClient httpClient,
        string appToken,
        string signingSecret,
        ILogger<SlackTriggerListener> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrEmpty(appToken);
        ArgumentException.ThrowIfNullOrEmpty(signingSecret);

        _httpClient = httpClient;
        _appToken = appToken;
        _signingSecret = signingSecret;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appToken);
    }

    /// <inheritdoc />
    public override TriggerSource Source => TriggerSource.Slack;

    /// <inheritdoc />
    protected override async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync($"{ApiBaseUrl}/auth.test", null, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<SlackApiResponse>(JsonOptions.SnakeCase, cancellationToken);

        if (result?.Ok != true)
            throw new InvalidOperationException($"Slack authentication failed: {result?.Error}");

        Logger.LogInformation("Connected to Slack workspace: {Team}", result.Team);
    }

    /// <inheritdoc />
    protected override Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    protected override Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        // Slack events are pushed via HTTP - wait indefinitely
        return Task.Delay(Timeout.Infinite, cancellationToken);
    }

    /// <summary>
    /// Processes an incoming Slack event from the HTTP server.
    /// </summary>
    /// <returns>Response body, or null if invalid.</returns>
    public string? ProcessSlackEvent(string body, string timestamp, string signature)
    {
        if (!VerifySignature(body, timestamp, signature))
        {
            Logger.LogWarning("Invalid Slack signature");
            return null;
        }

        var payload = JsonSerializer.Deserialize<SlackEventPayload>(body, JsonOptions.SnakeCase);
        if (payload is null) return null;

        // Handle URL verification challenge
        if (payload.Type == "url_verification")
            return payload.Challenge;

        // Handle event callbacks
        if (payload.Type == "event_callback" && payload.Event is not null)
            ProcessEvent(payload.Event.Value);

        return "ok";
    }

    /// <inheritdoc />
    public override async Task SendResponseAsync(
        IncomingMessage message,
        string response,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message.ChannelId))
        {
            Logger.LogWarning("Cannot send response: no channel ID");
            return;
        }

        var content = new StringContent(
            JsonSerializer.Serialize(new { channel = message.ChannelId, text = response }, JsonOptions.SnakeCase),
            Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync($"{ApiBaseUrl}/chat.postMessage", content, cancellationToken);
        var result = await httpResponse.Content.ReadFromJsonAsync<SlackApiResponse>(JsonOptions.SnakeCase, cancellationToken);

        if (result?.Ok != true)
            Logger.LogError("Failed to send Slack message: {Error}", result?.Error);
    }

    private bool VerifySignature(string body, string timestamp, string signature)
    {
        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
            return false;

        // Prevent replay attacks
        if (long.TryParse(timestamp, out var ts))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - ts) > TimestampWindowSeconds)
            {
                Logger.LogWarning("Slack timestamp too old");
                return false;
            }
        }

        // Compute expected signature
        var baseString = $"v0:{timestamp}:{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        var expected = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();

        return signature.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private void ProcessEvent(JsonElement eventData)
    {
        var type = eventData.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (type != "message") return;

        // Ignore subtypes (message_changed, bot_message, etc.)
        if (eventData.TryGetProperty("subtype", out _)) return;

        var incoming = IncomingMessageFactory.FromSlack(
            messageId: eventData.TryGetProperty("ts", out var ts) ? ts.GetString() ?? string.Empty : string.Empty,
            text: eventData.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty,
            username: null,
            userId: eventData.TryGetProperty("user", out var u) ? u.GetString() : null,
            channelId: eventData.TryGetProperty("channel", out var ch) ? ch.GetString() : null,
            channelName: null,
            rawPayload: eventData);

        Logger.LogDebug("Slack message from {User}: {Text}",
            incoming.SenderDisplayName,
            incoming.Text.Length > 50 ? incoming.Text[..50] + "..." : incoming.Text);

        PublishMessage(incoming);
    }

    #region Slack DTOs

    private sealed class SlackApiResponse
    {
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public string? Team { get; set; }
    }

    private sealed class SlackEventPayload
    {
        public string? Type { get; set; }
        public string? Challenge { get; set; }
        public JsonElement? Event { get; set; }
    }

    #endregion
}
