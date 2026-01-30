using System.Text.Json;

namespace WorkflowEngine.Triggers.Discord;

/// <summary>
/// Discord Gateway payload structure.
/// </summary>
public sealed class GatewayPayload
{
    /// <summary>
    /// Opcode for the payload.
    /// </summary>
    public int Op { get; set; }

    /// <summary>
    /// Event data.
    /// </summary>
    public JsonElement? D { get; set; }

    /// <summary>
    /// Sequence number (for resuming).
    /// </summary>
    public int? S { get; set; }

    /// <summary>
    /// Event name (for dispatch events).
    /// </summary>
    public string? T { get; set; }
}

/// <summary>
/// Discord user information.
/// </summary>
public sealed class DiscordUser
{
    /// <summary>
    /// User's username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// User's discriminator (legacy).
    /// </summary>
    public string? Discriminator { get; set; }

    /// <summary>
    /// User's unique ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Whether this user is a bot.
    /// </summary>
    public bool Bot { get; set; }
}
