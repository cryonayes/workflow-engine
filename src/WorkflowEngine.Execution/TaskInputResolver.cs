using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution.InputResolvers;

namespace WorkflowEngine.Execution;

/// <summary>
/// Resolves task input data from various sources using the strategy pattern.
/// All resolvers must be registered via dependency injection.
/// </summary>
public sealed class TaskInputResolver : ITaskInputResolver
{
    private readonly IReadOnlyDictionary<InputType, IInputTypeResolver> _resolvers;
    private readonly ILogger<TaskInputResolver> _logger;

    /// <summary>
    /// Initializes a new instance with the provided resolvers.
    /// </summary>
    /// <param name="resolvers">Collection of input type resolvers.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentException">Thrown when no resolvers are provided.</exception>
    public TaskInputResolver(
        IEnumerable<IInputTypeResolver> resolvers,
        ILogger<TaskInputResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        ArgumentNullException.ThrowIfNull(logger);

        _resolvers = resolvers.ToDictionary(r => r.SupportedType, r => r);
        _logger = logger;

        if (_resolvers.Count == 0)
        {
            throw new ArgumentException("At least one input type resolver must be provided", nameof(resolvers));
        }

        _logger.LogDebug("TaskInputResolver initialized with {Count} resolvers: {Types}",
            _resolvers.Count, string.Join(", ", _resolvers.Keys));
    }

    /// <inheritdoc />
    public async Task<byte[]?> ResolveInputAsync(
        WorkflowTask task,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var input = task.Input;
        if (input is null || input.Type == InputType.None)
            return null;

        _logger.LogDebug(
            "Resolving input for task '{TaskId}' with type {InputType}",
            task.Id, input.Type);

        if (_resolvers.TryGetValue(input.Type, out var resolver))
        {
            return await resolver.ResolveAsync(input, context, cancellationToken);
        }

        throw new ArgumentOutOfRangeException(nameof(input.Type), input.Type, "Unknown input type");
    }
}
