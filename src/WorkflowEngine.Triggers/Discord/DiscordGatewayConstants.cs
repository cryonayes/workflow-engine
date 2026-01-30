namespace WorkflowEngine.Triggers.Discord;

/// <summary>
/// Constants for Discord Gateway API.
/// </summary>
public static class DiscordGatewayConstants
{
    /// <summary>
    /// Discord API base URL.
    /// </summary>
    public const string ApiBaseUrl = "https://discord.com/api/v10";

    /// <summary>
    /// Discord Gateway WebSocket URL.
    /// </summary>
    public const string GatewayUrl = "wss://gateway.discord.gg/?v=10&encoding=json";

    /// <summary>
    /// Buffer size for WebSocket messages.
    /// </summary>
    public const int BufferSize = 16384;

    /// <summary>
    /// Gateway intents: GUILDS (512) | MESSAGE_CONTENT (32768).
    /// </summary>
    public const int DefaultIntents = 512 | 32768;

    /// <summary>
    /// Gateway opcodes.
    /// </summary>
    public static class OpCode
    {
        /// <summary>Dispatch - Receive events.</summary>
        public const int Dispatch = 0;

        /// <summary>Heartbeat - Send/receive heartbeats.</summary>
        public const int Heartbeat = 1;

        /// <summary>Identify - Start a new session.</summary>
        public const int Identify = 2;

        /// <summary>Resume - Resume a previous session.</summary>
        public const int Resume = 6;

        /// <summary>Reconnect - Server requests reconnection.</summary>
        public const int Reconnect = 7;

        /// <summary>Invalid Session - Session is invalid.</summary>
        public const int InvalidSession = 9;

        /// <summary>Hello - Received after connecting.</summary>
        public const int Hello = 10;
    }

    /// <summary>
    /// Gateway event types.
    /// </summary>
    public static class EventType
    {
        /// <summary>Session is ready.</summary>
        public const string Ready = "READY";

        /// <summary>Message was created.</summary>
        public const string MessageCreate = "MESSAGE_CREATE";
    }
}
