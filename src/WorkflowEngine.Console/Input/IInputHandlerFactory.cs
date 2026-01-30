using WorkflowEngine.Console.Rendering;
using WorkflowEngine.Console.State;

namespace WorkflowEngine.Console.Input;

/// <summary>
/// Factory for creating input handlers with complex dependencies.
/// </summary>
internal interface IInputHandlerFactory
{
    /// <summary>
    /// Creates an input handler with the specified callbacks.
    /// </summary>
    /// <param name="state">The renderer state.</param>
    /// <param name="graphView">The graph view renderer for line count.</param>
    /// <param name="onRelease">Callback to release step mode pause.</param>
    /// <param name="onInspect">Callback to inspect selected task.</param>
    /// <param name="onExport">Callback to export task output.</param>
    /// <param name="onCancelTask">Callback to cancel a running task.</param>
    /// <param name="onRetryTask">Optional callback to retry a failed task.</param>
    /// <returns>A configured input handler.</returns>
    IInputHandler Create(
        RendererState state,
        GraphViewRenderer graphView,
        Action onRelease,
        Action onInspect,
        Action<TaskInfo> onExport,
        Action<TaskInfo> onCancelTask,
        Action<TaskInfo>? onRetryTask = null);
}
