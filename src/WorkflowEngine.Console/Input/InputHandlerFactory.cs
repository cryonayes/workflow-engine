using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Console.Rendering;
using WorkflowEngine.Console.State;

namespace WorkflowEngine.Console.Input;

/// <summary>
/// Factory for creating input handlers with logging support.
/// </summary>
internal sealed class InputHandlerFactory : IInputHandlerFactory
{
    private readonly ILogger<InputHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputHandlerFactory"/> class.
    /// </summary>
    public InputHandlerFactory() : this(NullLogger<InputHandler>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance with a logger.
    /// </summary>
    /// <param name="logger">The logger for input handler diagnostics.</param>
    public InputHandlerFactory(ILogger<InputHandler> logger)
    {
        _logger = logger ?? NullLogger<InputHandler>.Instance;
    }

    /// <inheritdoc />
    public IInputHandler Create(
        RendererState state,
        GraphViewRenderer graphView,
        Action onRelease,
        Action onInspect,
        Action<TaskInfo> onExport,
        Action<TaskInfo> onCancelTask,
        Action<TaskInfo>? onRetryTask = null)
    {
        return new InputHandler(state, graphView, onRelease, onInspect, onExport, onCancelTask, onRetryTask, _logger);
    }
}
