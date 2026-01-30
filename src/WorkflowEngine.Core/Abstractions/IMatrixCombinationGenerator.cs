using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Generates matrix combinations from matrix configuration.
/// </summary>
public interface IMatrixCombinationGenerator
{
    /// <summary>
    /// Generates all valid combinations from the matrix configuration.
    /// </summary>
    /// <param name="matrix">The matrix configuration.</param>
    /// <returns>List of combinations, each represented as a dictionary of dimension name to value.</returns>
    IReadOnlyList<Dictionary<string, string>> Generate(MatrixConfig matrix);
}
