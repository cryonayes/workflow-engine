using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Handles matrix expression replacements in strings and objects.
/// </summary>
public interface IMatrixExpressionInterpolator
{
    /// <summary>
    /// Replaces matrix expressions in a string with concrete values.
    /// </summary>
    /// <param name="input">The input string containing matrix expressions.</param>
    /// <param name="matrixValues">The matrix values to interpolate.</param>
    /// <returns>The interpolated string.</returns>
    string Interpolate(string input, IReadOnlyDictionary<string, string> matrixValues);

    /// <summary>
    /// Interpolates matrix expressions in environment variables.
    /// </summary>
    /// <param name="environment">The environment variables dictionary.</param>
    /// <param name="matrixValues">The matrix values to interpolate.</param>
    /// <returns>A new dictionary with interpolated values.</returns>
    IReadOnlyDictionary<string, string> InterpolateEnvironment(
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> matrixValues);

    /// <summary>
    /// Interpolates matrix expressions in task input.
    /// </summary>
    /// <param name="input">The task input to interpolate.</param>
    /// <param name="matrixValues">The matrix values to interpolate.</param>
    /// <returns>A new task input with interpolated values, or null if input was null.</returns>
    TaskInput? InterpolateInput(TaskInput? input, IReadOnlyDictionary<string, string> matrixValues);
}
