using System.Text.RegularExpressions;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Matrix;

/// <summary>
/// Handles matrix expression replacements in strings and objects.
/// </summary>
public sealed partial class MatrixExpressionInterpolator : IMatrixExpressionInterpolator
{
    [GeneratedRegex(@"\$\{\{\s*matrix\.(\w+)\s*\}\}")]
    private static partial Regex MatrixExpressionPattern();

    /// <inheritdoc />
    public string Interpolate(string input, IReadOnlyDictionary<string, string> matrixValues)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(matrixValues);

        return MatrixExpressionPattern().Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return matrixValues.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> InterpolateEnvironment(
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> matrixValues)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(matrixValues);

        return environment.ToDictionary(
            kvp => kvp.Key,
            kvp => Interpolate(kvp.Value, matrixValues));
    }

    /// <inheritdoc />
    public TaskInput? InterpolateInput(TaskInput? input, IReadOnlyDictionary<string, string> matrixValues)
    {
        if (input is null)
            return null;

        ArgumentNullException.ThrowIfNull(matrixValues);

        return new TaskInput
        {
            Type = input.Type,
            Value = input.Value is not null
                ? Interpolate(input.Value, matrixValues)
                : null,
            FilePath = input.FilePath is not null
                ? Interpolate(input.FilePath, matrixValues)
                : null
        };
    }
}
