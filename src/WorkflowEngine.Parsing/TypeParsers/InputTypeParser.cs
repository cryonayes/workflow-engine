using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.TypeParsers;

/// <summary>
/// Parses string values into InputType enum values.
/// </summary>
public sealed class InputTypeParser : ITypeParser<InputType>
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static InputTypeParser Instance { get; } = new();

    private readonly Dictionary<string, InputType> _mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = InputType.Text,
        ["bytes"] = InputType.Bytes,
        ["file"] = InputType.File,
        ["pipe"] = InputType.Pipe,
        ["none"] = InputType.None,
        [""] = InputType.None
    };

    /// <inheritdoc />
    public InputType Parse(string? value) =>
        TryParse(value, out var result) ? result : InputType.None;

    /// <inheritdoc />
    public bool TryParse(string? value, out InputType result)
    {
        var key = value?.Trim() ?? string.Empty;
        return _mappings.TryGetValue(key, out result);
    }
}
