using System.Text;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.InputResolvers;

/// <summary>
/// Resolves text-based task input with variable interpolation.
/// </summary>
public sealed class TextInputResolver : IInputTypeResolver
{
    private readonly IExpressionInterpolator _interpolator;

    /// <inheritdoc />
    public InputType SupportedType => InputType.Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextInputResolver"/> class.
    /// </summary>
    public TextInputResolver(IExpressionInterpolator interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        _interpolator = interpolator;
    }

    /// <inheritdoc />
    public Task<byte[]?> ResolveAsync(TaskInput input, WorkflowContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(input.Value))
            return Task.FromResult<byte[]?>(null);

        var interpolated = _interpolator.Interpolate(input.Value, context);
        return Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(interpolated));
    }
}
