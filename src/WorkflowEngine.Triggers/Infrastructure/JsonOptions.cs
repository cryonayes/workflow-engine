using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkflowEngine.Triggers.Infrastructure;

/// <summary>
/// Shared JSON serialization options for trigger services.
/// </summary>
internal static class JsonOptions
{
    /// <summary>
    /// Options for snake_case JSON (Telegram, Discord, Slack APIs).
    /// </summary>
    public static JsonSerializerOptions SnakeCase { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Options for camelCase JSON.
    /// </summary>
    public static JsonSerializerOptions CamelCase { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Options for case-insensitive deserialization.
    /// </summary>
    public static JsonSerializerOptions Flexible { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
