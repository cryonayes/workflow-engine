using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Console.Rendering;
using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Input;

/// <summary>
/// Handles keyboard input for navigating and controlling the progress renderer.
/// </summary>
internal sealed class InputHandler : IInputHandler
{
    private readonly RendererState _state;
    private readonly GraphViewRenderer _graphView;
    private readonly Action _onRelease;
    private readonly Action _onInspect;
    private readonly Action<TaskInfo> _onExport;
    private readonly Action<TaskInfo> _onCancelTask;
    private readonly Action<TaskInfo>? _onRetryTask;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputHandler"/> class.
    /// </summary>
    /// <param name="state">The renderer state.</param>
    /// <param name="graphView">The graph view renderer for line count.</param>
    /// <param name="onRelease">Callback to release step mode pause.</param>
    /// <param name="onInspect">Callback to inspect selected task.</param>
    /// <param name="onExport">Callback to export task output.</param>
    /// <param name="onCancelTask">Callback to cancel a running task.</param>
    /// <param name="onRetryTask">Optional callback to retry a failed task.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public InputHandler(
        RendererState state,
        GraphViewRenderer graphView,
        Action onRelease,
        Action onInspect,
        Action<TaskInfo> onExport,
        Action<TaskInfo> onCancelTask,
        Action<TaskInfo>? onRetryTask = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(graphView);
        ArgumentNullException.ThrowIfNull(onRelease);
        ArgumentNullException.ThrowIfNull(onInspect);
        ArgumentNullException.ThrowIfNull(onExport);
        ArgumentNullException.ThrowIfNull(onCancelTask);

        _state = state;
        _graphView = graphView;
        _onRelease = onRelease;
        _onInspect = onInspect;
        _onExport = onExport;
        _onCancelTask = onCancelTask;
        _onRetryTask = onRetryTask;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public void Process()
    {
        try
        {
            // Process all available keys for responsive scrolling
            var keysProcessed = 0;
            const int maxKeysPerCycle = 10; // Prevent infinite loop on held keys

            while (System.Console.KeyAvailable && keysProcessed < maxKeysPerCycle)
            {
                var key = System.Console.ReadKey(intercept: true);
                keysProcessed++;

                if (_state.ShowingGraph) HandleGraphInput(key);
                else if (_state.InspectingTask != null) HandleInspectorInput(key);
                else HandleMainInput(key);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Input unavailable - log at debug level for diagnostics
            _logger.LogDebug(ex, "Console input unavailable");
        }
    }

    private void HandleMainInput(ConsoleKeyInfo key)
    {
        if (_state.Tasks.Count == 0) return;

        if (_state.IsPaused && key.Key == ConsoleKey.Spacebar)
        {
            _onRelease();
            return;
        }

        // Horizontal scroll step
        const int hScrollStep = 8;
        var contentHeight = MainContentHeight;

        switch (key.Key)
        {
            // Vertical navigation (task selection with auto-scroll)
            case ConsoleKey.UpArrow or ConsoleKey.K:
                _state.SelectedIndex = Math.Max(0, _state.SelectedIndex - 1);
                break;

            case ConsoleKey.DownArrow or ConsoleKey.J:
                _state.SelectedIndex = Math.Min(_state.Tasks.Count - 1, _state.SelectedIndex + 1);
                break;

            // Fast vertical navigation
            case ConsoleKey.PageUp:
                _state.SelectedIndex = Math.Max(0, _state.SelectedIndex - contentHeight);
                break;

            case ConsoleKey.PageDown:
                _state.SelectedIndex = Math.Min(_state.Tasks.Count - 1, _state.SelectedIndex + contentHeight);
                break;

            case ConsoleKey.Home:
                _state.SelectedIndex = 0;
                _state.MainHorizontalScroll = 0;
                break;

            case ConsoleKey.End:
                _state.SelectedIndex = _state.Tasks.Count - 1;
                break;

            // Horizontal scrolling
            case ConsoleKey.LeftArrow or ConsoleKey.H:
                _state.MainHorizontalScroll = Math.Max(0, _state.MainHorizontalScroll - hScrollStep);
                break;

            case ConsoleKey.RightArrow or ConsoleKey.L:
                _state.MainHorizontalScroll += hScrollStep; // Will be clamped by renderer
                break;

            case ConsoleKey.Enter:
                _onInspect();
                break;

            case ConsoleKey.E:
                ExportSelectedTaskOutput();
                break;

            case ConsoleKey.C:
                CancelSelectedTask();
                break;

            case ConsoleKey.R:
                RetrySelectedTask();
                break;

            case ConsoleKey.G:
                _state.ShowingGraph = true;
                _state.GraphScroll = 0;
                _state.GraphHorizontalScroll = 0;
                break;

            case ConsoleKey.Q when !_state.IsRunning:
                _state.ExitRequested = true;
                break;
        }
    }

    private void HandleInspectorInput(ConsoleKeyInfo key)
    {
        var task = _state.InspectingTask!;
        var contentHeight = ContentHeight;
        var maxVScroll = Math.Max(0, task.Output.Count - contentHeight);

        // Horizontal scroll step
        const int hScrollStep = 8;

        switch (key.Key)
        {
            // Vertical scrolling
            case ConsoleKey.UpArrow or ConsoleKey.K:
                _state.InspectScroll = Math.Max(0, _state.InspectScroll - 1);
                break;

            case ConsoleKey.DownArrow or ConsoleKey.J:
                _state.InspectScroll = Math.Min(maxVScroll, _state.InspectScroll + 1);
                break;

            case ConsoleKey.PageUp:
                _state.InspectScroll = Math.Max(0, _state.InspectScroll - contentHeight);
                break;

            case ConsoleKey.PageDown:
                _state.InspectScroll = Math.Min(maxVScroll, _state.InspectScroll + contentHeight);
                break;

            // Horizontal scrolling
            case ConsoleKey.LeftArrow or ConsoleKey.H:
                _state.InspectHorizontalScroll = Math.Max(0, _state.InspectHorizontalScroll - hScrollStep);
                break;

            case ConsoleKey.RightArrow or ConsoleKey.L:
                _state.InspectHorizontalScroll += hScrollStep; // Will be clamped by renderer
                break;

            // Jump to start/end
            case ConsoleKey.Home:
                _state.InspectScroll = 0;
                _state.InspectHorizontalScroll = 0;
                break;

            case ConsoleKey.End:
                _state.InspectScroll = maxVScroll;
                break;

            case ConsoleKey.Spacebar when _state.IsPaused:
                _onRelease();
                _state.InspectingTask = null;
                _state.InspectScroll = 0;
                _state.InspectHorizontalScroll = 0;
                break;

            case ConsoleKey.C:
                if (task.Status == ExecutionStatus.Running)
                {
                    _onCancelTask(task);
                }
                break;

            case ConsoleKey.R:
                if (task.Status == ExecutionStatus.Failed || task.Status == ExecutionStatus.TimedOut)
                {
                    _onRetryTask?.Invoke(task);
                }
                break;

            case ConsoleKey.E:
                _onExport(task);
                break;

            case ConsoleKey.Escape or ConsoleKey.Q or ConsoleKey.Backspace:
                _state.InspectingTask = null;
                _state.InspectScroll = 0;
                _state.InspectHorizontalScroll = 0;
                break;
        }
    }

    private void HandleGraphInput(ConsoleKeyInfo key)
    {
        var size = TerminalInfo.Size;
        var contentHeight = Math.Max(1, size.Height - 3);
        var contentWidth = size.Width - 2;
        var maxVScroll = Math.Max(0, _graphView.LineCount - contentHeight);
        var maxHScroll = Math.Max(0, _graphView.MaxLineWidth - contentWidth);

        // Horizontal scroll step (larger for faster scrolling)
        const int hScrollStep = 8;

        switch (key.Key)
        {
            // Vertical scrolling
            case ConsoleKey.UpArrow or ConsoleKey.K:
                _state.GraphScroll = Math.Max(0, _state.GraphScroll - 1);
                break;

            case ConsoleKey.DownArrow or ConsoleKey.J:
                _state.GraphScroll = Math.Min(maxVScroll, _state.GraphScroll + 1);
                break;

            case ConsoleKey.PageUp:
                _state.GraphScroll = Math.Max(0, _state.GraphScroll - contentHeight);
                break;

            case ConsoleKey.PageDown:
                _state.GraphScroll = Math.Min(maxVScroll, _state.GraphScroll + contentHeight);
                break;

            // Horizontal scrolling
            case ConsoleKey.LeftArrow or ConsoleKey.H:
                _state.GraphHorizontalScroll = Math.Max(0, _state.GraphHorizontalScroll - hScrollStep);
                break;

            case ConsoleKey.RightArrow or ConsoleKey.L:
                _state.GraphHorizontalScroll = Math.Min(maxHScroll, _state.GraphHorizontalScroll + hScrollStep);
                break;

            // Jump to start/end
            case ConsoleKey.Home:
                _state.GraphScroll = 0;
                _state.GraphHorizontalScroll = 0;
                break;

            case ConsoleKey.End:
                _state.GraphScroll = maxVScroll;
                break;

            case ConsoleKey.Spacebar when _state.IsPaused:
                _onRelease();
                _state.ShowingGraph = false;
                break;

            case ConsoleKey.Escape or ConsoleKey.Q or ConsoleKey.Backspace or ConsoleKey.G:
                _state.ShowingGraph = false;
                _state.GraphHorizontalScroll = 0; // Reset horizontal scroll on exit
                break;
        }
    }

    private void CancelSelectedTask()
    {
        var task = _state.Tasks[_state.SelectedIndex];
        if (task.Status == ExecutionStatus.Running)
        {
            _onCancelTask(task);
        }
    }

    private void RetrySelectedTask()
    {
        var task = _state.Tasks[_state.SelectedIndex];
        if ((task.Status == ExecutionStatus.Failed || task.Status == ExecutionStatus.TimedOut) && _onRetryTask is not null)
        {
            _onRetryTask(task);
        }
    }

    private void ExportSelectedTaskOutput()
    {
        var task = _state.Tasks[_state.SelectedIndex];
        _onExport(task);
    }

    private static int ContentHeight => Math.Max(1, TerminalInfo.Size.Height - 3);

    private static int MainContentHeight => Math.Max(1, TerminalInfo.Size.Height - 5);
}
