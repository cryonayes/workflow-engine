using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.TypeParsers;

/// <summary>
/// Parses string values into OutputType enum values.
/// </summary>
public sealed class OutputTypeParser : ITypeParser<OutputType>
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static OutputTypeParser Instance { get; } = new();

    private readonly Dictionary<string, OutputType> _mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = OutputType.String,
        ["bytes"] = OutputType.Bytes,
        ["file"] = OutputType.File,
        ["stream"] = OutputType.Stream,
        [""] = OutputType.String
    };

    /// <inheritdoc />
    public OutputType Parse(string? value) =>
        TryParse(value, out var result) ? result : OutputType.String;

    /// <inheritdoc />
    public bool TryParse(string? value, out OutputType result)
    {
        var key = value?.Trim() ?? string.Empty;
        return _mappings.TryGetValue(key, out result);
    }
}
