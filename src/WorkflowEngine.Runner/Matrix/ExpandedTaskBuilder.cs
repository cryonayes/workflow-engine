using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner.Matrix;

/// <summary>
/// Creates expanded task instances from a template and matrix values.
/// </summary>
public sealed class ExpandedTaskBuilder : IExpandedTaskBuilder
{
    private readonly IMatrixExpressionInterpolator _interpolator;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ExpandedTaskBuilder(IMatrixExpressionInterpolator interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        _interpolator = interpolator;
    }

    /// <inheritdoc />
    public WorkflowTask Build(WorkflowTask template, Dictionary<string, string> matrixValues)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(matrixValues);

        var expandedId = _interpolator.Interpolate(template.Id, matrixValues);

        // If ID doesn't contain matrix expressions, generate a unique ID
        if (expandedId == template.Id)
        {
            expandedId = GenerateTaskId(template.Id, matrixValues);
        }

        return new WorkflowTask
        {
            Id = expandedId,
            Name = template.Name is not null
                ? _interpolator.Interpolate(template.Name, matrixValues)
                : null,
            Run = _interpolator.Interpolate(template.Run, matrixValues),
            Shell = template.Shell,
            WorkingDirectory = template.WorkingDirectory is not null
                ? _interpolator.Interpolate(template.WorkingDirectory, matrixValues)
                : null,
            Environment = _interpolator.InterpolateEnvironment(template.Environment, matrixValues),
            If = template.If is not null
                ? _interpolator.Interpolate(template.If, matrixValues)
                : null,
            Input = _interpolator.InterpolateInput(template.Input, matrixValues),
            Output = template.Output,
            TimeoutMs = template.TimeoutMs,
            ContinueOnError = template.ContinueOnError,
            RetryCount = template.RetryCount,
            RetryDelayMs = template.RetryDelayMs,
            DependsOn = template.DependsOn,
            Matrix = null, // Clear matrix config on expanded task
            MatrixValues = matrixValues
        };
    }

    /// <inheritdoc />
    public string GenerateTaskId(string baseId, IReadOnlyDictionary<string, string> matrixValues)
    {
        ArgumentNullException.ThrowIfNull(baseId);
        ArgumentNullException.ThrowIfNull(matrixValues);

        var suffix = string.Join("-", matrixValues.Values.Select(SanitizeIdComponent));
        return $"{baseId}-{suffix}";
    }

    /// <inheritdoc />
    public string SanitizeIdComponent(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Replace special characters with underscores and trim
        return new string(value
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray())
            .Trim('_');
    }
}
