using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Matrix;

/// <summary>
/// Generates matrix combinations from matrix configuration.
/// </summary>
public sealed class MatrixCombinationGenerator : IMatrixCombinationGenerator
{
    /// <inheritdoc />
    public IReadOnlyList<Dictionary<string, string>> Generate(MatrixConfig matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var combinations = new List<Dictionary<string, string>>();

        // Generate Cartesian product of dimensions
        if (matrix.HasDimensions)
        {
            var cartesianProduct = CartesianProduct(matrix.Dimensions);
            combinations.AddRange(cartesianProduct);
        }

        // Apply exclusions
        combinations = ApplyExclusions(combinations, matrix.Exclude);

        // Apply inclusions
        combinations = ApplyInclusions(combinations, matrix.Include);

        return combinations;
    }

    /// <summary>
    /// Computes the Cartesian product of all dimension values.
    /// </summary>
    private static List<Dictionary<string, string>> CartesianProduct(
        IReadOnlyDictionary<string, IReadOnlyList<string>> dimensions)
    {
        var result = new List<Dictionary<string, string>> { new() };

        foreach (var (key, values) in dimensions)
        {
            var newResult = new List<Dictionary<string, string>>();
            foreach (var combo in result)
            {
                foreach (var value in values)
                {
                    var newCombo = new Dictionary<string, string>(combo) { [key] = value };
                    newResult.Add(newCombo);
                }
            }
            result = newResult;
        }

        return result;
    }

    /// <summary>
    /// Removes combinations that match any exclusion pattern.
    /// </summary>
    private static List<Dictionary<string, string>> ApplyExclusions(
        List<Dictionary<string, string>> combinations,
        IReadOnlyList<IReadOnlyDictionary<string, string>> exclusions)
    {
        if (exclusions.Count == 0)
            return combinations;

        return combinations
            .Where(combo => !exclusions.Any(excl => MatchesCombination(combo, excl)))
            .ToList();
    }

    /// <summary>
    /// Adds inclusion combinations, merging with existing if there's a partial match.
    /// </summary>
    private static List<Dictionary<string, string>> ApplyInclusions(
        List<Dictionary<string, string>> combinations,
        IReadOnlyList<IReadOnlyDictionary<string, string>> inclusions)
    {
        if (inclusions.Count == 0)
            return combinations;

        foreach (var inclusion in inclusions)
        {
            // Check if this inclusion matches any existing combination
            var matched = false;
            foreach (var combo in combinations)
            {
                if (PartialMatchesCombination(combo, inclusion))
                {
                    // Merge additional keys from inclusion
                    foreach (var (key, value) in inclusion)
                    {
                        combo[key] = value;
                    }
                    matched = true;
                }
            }

            // If no match, add as a new combination
            if (!matched)
            {
                combinations.Add(new Dictionary<string, string>(
                    inclusion.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
            }
        }

        return combinations;
    }

    /// <summary>
    /// Checks if a combination matches all keys in a pattern.
    /// </summary>
    private static bool MatchesCombination(
        Dictionary<string, string> combination,
        IReadOnlyDictionary<string, string> pattern)
    {
        return pattern.All(kvp =>
            combination.TryGetValue(kvp.Key, out var value) &&
            value.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a combination partially matches (for inclusion merging).
    /// </summary>
    private static bool PartialMatchesCombination(
        Dictionary<string, string> combination,
        IReadOnlyDictionary<string, string> pattern)
    {
        // Check if all existing keys in combination that are also in pattern match
        return combination.All(kvp =>
            !pattern.TryGetValue(kvp.Key, out var patternValue) ||
            kvp.Value.Equals(patternValue, StringComparison.OrdinalIgnoreCase));
    }
}
