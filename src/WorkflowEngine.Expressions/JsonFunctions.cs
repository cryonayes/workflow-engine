using System.Text.Json;
using System.Text.RegularExpressions;

namespace WorkflowEngine.Expressions;

/// <summary>
/// Provides JSON parsing and property access functions for expressions.
/// </summary>
/// <remarks>
/// Supports the following patterns:
/// <list type="bullet">
/// <item><c>fromJson(value).property</c> - Access a property</item>
/// <item><c>fromJson(value).nested.property</c> - Access nested properties</item>
/// <item><c>fromJson(value).array[0]</c> - Access array element by index</item>
/// <item><c>fromJson(value).array[0].property</c> - Access property of array element</item>
/// </list>
/// </remarks>
public sealed partial class JsonFunctions : IJsonFunctions
{
    [GeneratedRegex(@"^fromJson\((.+?)\)(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex FromJsonPattern();

    [GeneratedRegex(@"^\[(\d+)\]")]
    private static partial Regex ArrayIndexPattern();

    /// <inheritdoc />
    public bool IsFromJsonExpression(string expression)
    {
        return expression.TrimStart().StartsWith("fromJson(", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public (string innerExpression, string propertyPath)? ParseFromJsonExpression(string expression)
    {
        var match = FromJsonPattern().Match(expression.Trim());
        if (!match.Success)
            return null;

        var innerExpression = match.Groups[1].Value.Trim();
        var propertyPath = match.Groups[2].Value.Trim();

        // Remove leading dot if present
        if (propertyPath.StartsWith('.'))
            propertyPath = propertyPath[1..];

        return (innerExpression, propertyPath);
    }

    /// <inheritdoc />
    public string ExtractJsonProperty(string json, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(propertyPath))
            return json;

        try
        {
            using var document = JsonDocument.Parse(json);
            var element = NavigateToProperty(document.RootElement, propertyPath);
            return ElementToString(element);
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Navigates to a property in a JSON element using a dot-notation path.
    /// </summary>
    private static JsonElement NavigateToProperty(JsonElement element, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
            return element;

        var current = element;
        var remaining = propertyPath;

        while (!string.IsNullOrEmpty(remaining))
        {
            // Check for array index at start: [0]
            var arrayMatch = ArrayIndexPattern().Match(remaining);
            if (arrayMatch.Success)
            {
                var index = int.Parse(arrayMatch.Groups[1].Value);
                if (current.ValueKind != JsonValueKind.Array || index >= current.GetArrayLength())
                    return default;

                current = current[index];
                remaining = remaining[arrayMatch.Length..];

                // Skip trailing dot if present
                if (remaining.StartsWith('.'))
                    remaining = remaining[1..];

                continue;
            }

            // Find next segment (property name)
            var dotIndex = remaining.IndexOf('.');
            var bracketIndex = remaining.IndexOf('[');

            int segmentEnd;
            if (dotIndex == -1 && bracketIndex == -1)
                segmentEnd = remaining.Length;
            else if (dotIndex == -1)
                segmentEnd = bracketIndex;
            else if (bracketIndex == -1)
                segmentEnd = dotIndex;
            else
                segmentEnd = Math.Min(dotIndex, bracketIndex);

            var propertyName = remaining[..segmentEnd];

            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(propertyName, out var next))
                return default;

            current = next;
            remaining = remaining[segmentEnd..];

            // Skip dot separator
            if (remaining.StartsWith('.'))
                remaining = remaining[1..];
        }

        return current;
    }

    /// <summary>
    /// Converts a JSON element to its string representation.
    /// </summary>
    private static string ElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Undefined => string.Empty,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array or JsonValueKind.Object => element.GetRawText(),
            _ => string.Empty
        };
    }
}
