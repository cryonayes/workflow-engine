using System.Text;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.InputResolvers;

/// <summary>
/// Resolves piped task input from previous task output.
/// </summary>
public sealed class PipeInputResolver : IInputTypeResolver
{
    private readonly IExpressionInterpolator _interpolator;
    private readonly ILogger<PipeInputResolver> _logger;

    /// <inheritdoc />
    public InputType SupportedType => InputType.Pipe;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeInputResolver"/> class.
    /// </summary>
    public PipeInputResolver(IExpressionInterpolator interpolator, ILogger<PipeInputResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        ArgumentNullException.ThrowIfNull(logger);

        _interpolator = interpolator;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<byte[]?> ResolveAsync(TaskInput input, WorkflowContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(input.Value))
            return Task.FromResult<byte[]?>(null);

        // Interpolate the value to get the actual piped content
        var interpolated = _interpolator.Interpolate(input.Value, context);

        if (string.IsNullOrEmpty(interpolated))
        {
            _logger.LogDebug("Piped input resolved to empty string");
            return Task.FromResult<byte[]?>(null);
        }

        return Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(interpolated));
    }
}
