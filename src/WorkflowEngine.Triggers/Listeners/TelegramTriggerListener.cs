using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Infrastructure;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Listeners;

/// <summary>
/// Telegram bot listener using long polling.
/// </summary>
public sealed class TelegramTriggerListener : BaseTriggerListener
{
    private const string ApiBaseUrl = "https://api.telegram.org/bot";
    private const int LongPollTimeoutSeconds = 30;

    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private long _lastUpdateId;

    /// <summary>
    /// Initializes a new instance of the TelegramTriggerListener.
    /// </summary>
    public TelegramTriggerListener(
        HttpClient httpClient,
        string botToken,
        ILogger<TelegramTriggerListener> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrEmpty(botToken);

        _httpClient = httpClient;
        _botToken = botToken;
    }

    /// <inheritdoc />
    public override TriggerSource Source => TriggerSource.Telegram;

    /// <inheritdoc />
    protected override async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var response = await CallApiAsync<TelegramUser>("getMe", null, cancellationToken);

        if (response?.Result is null)
            throw new InvalidOperationException("Failed to verify Telegram bot token");

        Logger.LogInformation("Connected to Telegram as @{Username}", response.Result.Username);
    }

    /// <inheritdoc />
    protected override Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    protected override async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var payload = new
        {
            offset = _lastUpdateId + 1,
            timeout = LongPollTimeoutSeconds,
            allowed_updates = new[] { "message" }
        };

        var response = await CallApiAsync<TelegramUpdate[]>("getUpdates", payload, cancellationToken);

        if (response?.Result is null)
            return;

        foreach (var update in response.Result)
        {
            ProcessUpdate(update);
            _lastUpdateId = update.UpdateId;
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
            Logger.LogWarning("Cannot send response: no chat ID");
            return;
        }

        await CallApiAsync<TelegramMessage>("sendMessage", new
        {
            chat_id = message.ChannelId,
            text = response,
            parse_mode = "Markdown"
        }, cancellationToken);
    }

    private async Task<TelegramResponse<T>?> CallApiAsync<T>(
        string method,
        object? payload,
        CancellationToken cancellationToken)
    {
        var url = $"{ApiBaseUrl}{_botToken}/{method}";

        var response = payload is null
            ? await _httpClient.GetAsync(url, cancellationToken)
            : await _httpClient.PostAsJsonAsync(url, payload, JsonOptions.SnakeCase, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TelegramResponse<T>>(JsonOptions.SnakeCase, cancellationToken);
    }

    private void ProcessUpdate(TelegramUpdate update)
    {
        if (update.Message is not { Text: not null } msg)
            return;

        var incoming = IncomingMessageFactory.FromTelegram(
            messageId: msg.MessageId.ToString(),
            text: msg.Text,
            username: msg.From?.Username,
            userId: msg.From?.Id.ToString(),
            chatId: msg.Chat.Id.ToString(),
            rawPayload: update);

        Logger.LogDebug("Telegram message from {User}: {Text}",
            incoming.SenderDisplayName,
            TruncateText(incoming.Text, 50));

        PublishMessage(incoming);
    }

    private static string TruncateText(string text, int maxLength) =>
        text.Length > maxLength ? text[..maxLength] + "..." : text;

    #region Telegram DTOs

    private sealed class TelegramResponse<T>
    {
        public bool Ok { get; set; }
        public T? Result { get; set; }
    }

    private sealed class TelegramUpdate
    {
        [JsonPropertyName("update_id")]
        public long UpdateId { get; set; }
        public TelegramMessage? Message { get; set; }
    }

    private sealed class TelegramMessage
    {
        [JsonPropertyName("message_id")]
        public long MessageId { get; set; }
        public TelegramUser? From { get; set; }
        public TelegramChat Chat { get; set; } = null!;
        public string? Text { get; set; }
    }

    private sealed class TelegramUser
    {
        public long Id { get; set; }
        public string? Username { get; set; }
    }

    private sealed class TelegramChat
    {
        public long Id { get; set; }
    }

    #endregion
}
