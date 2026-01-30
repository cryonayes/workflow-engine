using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Parsing.Mappers;

/// <summary>
/// Maps matrix configuration from YAML dictionary to domain model.
/// </summary>
internal static class MatrixConfigMapper
{
    /// <summary>
    /// Maps a matrix dictionary to a MatrixConfig.
    /// </summary>
    /// <param name="matrixDto">The raw matrix dictionary from YAML.</param>
    /// <returns>The mapped config, or null if the dictionary is null or empty.</returns>
    public static MatrixConfig? Map(Dictionary<string, object>? matrixDto)
    {
        if (matrixDto is null || matrixDto.Count == 0)
            return null;

        var dimensions = new Dictionary<string, IReadOnlyList<string>>();
        var include = new List<IReadOnlyDictionary<string, string>>();
        var exclude = new List<IReadOnlyDictionary<string, string>>();

        foreach (var (key, value) in matrixDto)
        {
            if (key.Equals("include", StringComparison.OrdinalIgnoreCase))
            {
                include.AddRange(ParseCombinations(value));
            }
            else if (key.Equals("exclude", StringComparison.OrdinalIgnoreCase))
            {
                exclude.AddRange(ParseCombinations(value));
            }
            else
            {
                dimensions[key] = ParseDimensionValues(value);
            }
        }

        if (dimensions.Count == 0 && include.Count == 0)
            return null;

        return new MatrixConfig
        {
            Dimensions = dimensions,
            Include = include,
            Exclude = exclude
        };
    }

    /// <summary>
    /// Parses dimension values from a YAML value.
    /// </summary>
    private static IReadOnlyList<string> ParseDimensionValues(object value)
    {
        return value switch
        {
            IEnumerable<object> list => list.Select(v => v?.ToString() ?? string.Empty).ToList(),
            string str => [str],
            _ => [value?.ToString() ?? string.Empty]
        };
    }

    /// <summary>
    /// Parses matrix include/exclude combinations from a YAML value.
    /// </summary>
    private static IEnumerable<IReadOnlyDictionary<string, string>> ParseCombinations(object value)
    {
        if (value is not IEnumerable<object> list)
            yield break;

        foreach (var item in list)
        {
            if (item is IDictionary<object, object> dict)
            {
                yield return dict.ToDictionary(
                    kvp => kvp.Key?.ToString() ?? string.Empty,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
            }
        }
    }
}
