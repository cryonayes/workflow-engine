using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Discord;
using WorkflowEngine.Triggers.Infrastructure;
using WorkflowEngine.Triggers.Models;
using static WorkflowEngine.Triggers.Discord.DiscordGatewayConstants;

namespace WorkflowEngine.Triggers.Listeners;

/// <summary>
/// Discord bot listener using Gateway WebSocket.
/// </summary>
public sealed class DiscordTriggerListener : BaseTriggerListener
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private ClientWebSocket? _webSocket;
    private Timer? _heartbeatTimer;
    private string? _sessionId;
    private int _lastSequence;

    /// <summary>
    /// Initializes a new instance of the DiscordTriggerListener.
    /// </summary>
    public DiscordTriggerListener(
        HttpClient httpClient,
        string botToken,
        ILogger<DiscordTriggerListener> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrEmpty(botToken);

        _httpClient = httpClient;
        _botToken = botToken;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", botToken);
    }

    /// <inheritdoc />
    public override TriggerSource Source => TriggerSource.Discord;

    /// <inheritdoc />
    protected override async Task ConnectAsync(CancellationToken cancellationToken)
    {
        // Verify bot token
        var user = await _httpClient.GetFromJsonAsync<DiscordUser>(
            $"{ApiBaseUrl}/users/@me", JsonOptions.SnakeCase, cancellationToken);

        Logger.LogInformation("Verified Discord bot: {Username}#{Discriminator}",
            user?.Username, user?.Discriminator);

        // Connect to Gateway
        await ConnectWebSocketAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;

        if (_webSocket is { State: WebSocketState.Open })
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutdown", cancellationToken);
        }

        _webSocket?.Dispose();
        _webSocket = null;
    }

    /// <inheritdoc />
    protected override async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        if (_webSocket is not { State: WebSocketState.Open })
        {
            await ReconnectAsync(cancellationToken);
            return;
        }

        var buffer = new byte[BufferSize];
        var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            Logger.LogWarning("Discord Gateway closed: {Status}", result.CloseStatus);
            await ReconnectAsync(cancellationToken);
            return;
        }

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await HandleGatewayPayloadAsync(json, cancellationToken);
        }
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
            JsonSerializer.Serialize(new { content = response }, JsonOptions.SnakeCase),
            Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync(
            $"{ApiBaseUrl}/channels/{message.ChannelId}/messages", content, cancellationToken);

        httpResponse.EnsureSuccessStatusCode();
    }

    private async Task ConnectWebSocketAsync(CancellationToken cancellationToken)
    {
        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(new Uri(GatewayUrl), cancellationToken);
        Logger.LogDebug("Connected to Discord Gateway");
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        SetConnected(false);
        _heartbeatTimer?.Dispose();

        await Task.Delay(GetBackoffDelay(1), cancellationToken);
        await ConnectWebSocketAsync(cancellationToken);
    }

    private async Task HandleGatewayPayloadAsync(string json, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<GatewayPayload>(json, JsonOptions.SnakeCase);
        if (payload is null) return;

        if (payload.S.HasValue)
            _lastSequence = payload.S.Value;

        switch (payload.Op)
        {
            case OpCode.Hello:
                await HandleHelloAsync(payload, cancellationToken);
                break;
            case OpCode.Dispatch:
                HandleDispatch(payload);
                break;
            case OpCode.Reconnect:
                await ReconnectAsync(cancellationToken);
                break;
            case OpCode.InvalidSession:
                _sessionId = null;
                await SendIdentifyAsync(cancellationToken);
                break;
        }
    }

    private async Task HandleHelloAsync(GatewayPayload payload, CancellationToken cancellationToken)
    {
        var interval = payload.D?.GetProperty("heartbeat_interval").GetInt32() ?? 45000;

        _heartbeatTimer?.Dispose();
        _heartbeatTimer = new Timer(_ => _ = SendHeartbeatAsync(), null, interval, interval);

        if (_sessionId is not null)
            await SendResumeAsync(cancellationToken);
        else
            await SendIdentifyAsync(cancellationToken);
    }

    private void HandleDispatch(GatewayPayload payload)
    {
        switch (payload.T)
        {
            case EventType.Ready:
                _sessionId = payload.D?.GetProperty("session_id").GetString();
                SetConnected(true);
                Logger.LogInformation("Discord session ready");
                break;

            case EventType.MessageCreate:
                ProcessMessage(payload.D);
                break;
        }
    }

    private void ProcessMessage(JsonElement? data)
    {
        if (data is null) return;

        var d = data.Value;

        // Ignore bot messages
        if (d.TryGetProperty("author", out var author) &&
            author.TryGetProperty("bot", out var isBot) && isBot.GetBoolean())
            return;

        var incoming = IncomingMessageFactory.FromDiscord(
            messageId: d.GetProperty("id").GetString() ?? string.Empty,
            text: d.TryGetProperty("content", out var c) ? c.GetString() ?? string.Empty : string.Empty,
            username: author.TryGetProperty("username", out var u) ? u.GetString() : null,
            userId: author.TryGetProperty("id", out var id) ? id.GetString() : null,
            channelId: d.TryGetProperty("channel_id", out var ch) ? ch.GetString() : null,
            channelName: null,
            rawPayload: d);

        Logger.LogDebug("Discord message from {User}: {Text}",
            incoming.SenderDisplayName,
            incoming.Text.Length > 50 ? incoming.Text[..50] + "..." : incoming.Text);

        PublishMessage(incoming);
    }

    private async Task SendIdentifyAsync(CancellationToken cancellationToken)
    {
        await SendGatewayAsync(new
        {
            op = OpCode.Identify,
            d = new
            {
                token = _botToken,
                intents = DefaultIntents,
                properties = new { os = "linux", browser = "workflow-engine", device = "workflow-engine" }
            }
        }, cancellationToken);
    }

    private async Task SendResumeAsync(CancellationToken cancellationToken)
    {
        await SendGatewayAsync(new
        {
            op = OpCode.Resume,
            d = new { token = _botToken, session_id = _sessionId, seq = _lastSequence }
        }, cancellationToken);
    }

    private async Task SendHeartbeatAsync()
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        try
        {
            await SendGatewayAsync(new { op = OpCode.Heartbeat, d = _lastSequence > 0 ? (int?)_lastSequence : null }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    private async Task SendGatewayAsync(object payload, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        var json = JsonSerializer.Serialize(payload, JsonOptions.SnakeCase);
        await _webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancellationToken);
    }
}
