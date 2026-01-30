using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WorkflowEngine.Console.Abstractions;
using WorkflowEngine.Console.Events;
using WorkflowEngine.Console.Export;
using WorkflowEngine.Console.Input;
using WorkflowEngine.Console.Notifications;
using WorkflowEngine.Console.Rendering;
using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console;

/// <summary>
/// Renders workflow execution progress using direct ANSI rendering for reliable resize handling.
/// </summary>
public sealed class WorkflowProgressRenderer : IProgressRenderer, IStepController
{
    private readonly RendererState _state = new();
    private readonly ToastManager _toasts;
    private readonly IOutputExporter _exporter;
    private readonly ITerminalProvider _terminalProvider;
    private readonly ILogger<WorkflowProgressRenderer> _logger;
    private readonly MainViewRenderer _mainView = new();
    private readonly InspectorViewRenderer _inspectorView = new();
    private readonly GraphViewRenderer _graphView = new();
    private readonly SemaphoreSlim _stepGate = new(0, 1);
    private readonly object _lock = new();
    private readonly IInputHandler _inputHandler;
    private readonly WorkflowEventAggregator _eventAggregator;

    private IAnsiConsole? _console;
    private Timer? _refreshTimer;
    private bool _disposed;
    private (int Width, int Height) _lastSize;

    private Workflow? _workflow;
    private ExecutionPlan? _executionPlan;
    private WorkflowContext? _workflowContext;
    private ITaskRetrier? _taskRetrier;
    private int _graphRenderedWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowProgressRenderer"/> class.
    /// </summary>
    public WorkflowProgressRenderer()
        : this(
            new ToastManager(),
            new AnsiTerminalProvider(),
            new OutputExporter(new ToastManager()),
            new InputHandlerFactory(),
            NullLogger<WorkflowProgressRenderer>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance with dependencies for DI.
    /// </summary>
    internal WorkflowProgressRenderer(
        ToastManager toasts,
        ITerminalProvider terminalProvider,
        IOutputExporter exporter,
        IInputHandlerFactory inputHandlerFactory,
        ILogger<WorkflowProgressRenderer> logger)
    {
        ArgumentNullException.ThrowIfNull(toasts);
        ArgumentNullException.ThrowIfNull(terminalProvider);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(inputHandlerFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _toasts = toasts;
        _terminalProvider = terminalProvider;
        _logger = logger;
        _exporter = exporter;
        _eventAggregator = new WorkflowEventAggregator(_state);
        _inputHandler = inputHandlerFactory.Create(
            _state,
            _graphView,
            Release,
            InspectSelectedTask,
            task => _exporter.ExportTaskOutput(task),
            CancelTask,
            RetryTask);
    }

    /// <inheritdoc />
    public void SetExecutionPlan(ExecutionPlan plan, Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(workflow);

        lock (_lock)
        {
            _executionPlan = plan;
            _workflow = workflow;

            foreach (var wave in plan.Waves)
                AddWave(wave.WaveIndex, wave.Tasks, isAlways: false);

            if (plan.AlwaysTasks.Count > 0)
                AddWave(plan.WaveCount, plan.AlwaysTasks, isAlways: true);

            RenderGraph(_terminalProvider.GetSize().Width);
        }
    }

    /// <summary>
    /// Sets the workflow context for task cancellation support.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    public void SetWorkflowContext(WorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        lock (_lock)
        {
            _workflowContext = context;
        }
    }

    /// <summary>
    /// Sets whether step mode is enabled.
    /// </summary>
    public void SetStepMode(bool enabled)
    {
        lock (_lock)
        {
            _state.StepMode = enabled;
        }
    }

    #region IStepController

    /// <inheritdoc />
    public Task WaitAsync(CancellationToken cancellationToken) =>
        _stepGate.WaitAsync(cancellationToken);

    /// <inheritdoc />
    public void Release()
    {
        lock (_lock)
        {
            _state.IsPaused = false;
        }

        if (_stepGate.CurrentCount == 0)
            _stepGate.Release();
    }

    #endregion

    /// <summary>
    /// Sets the live display context and starts rendering.
    /// </summary>
    public void SetLiveContext(LiveDisplayContext context)
    {
        _console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(System.Console.Out)
        });

        _lastSize = _terminalProvider.GetSize();
        _terminalProvider.HideCursor();
        _terminalProvider.ClearScreen();

        lock (_lock) { Render(); }
        _refreshTimer = new Timer(_ => Refresh(), null, LayoutConstants.RefreshMs, LayoutConstants.RefreshMs);
    }

    /// <summary>
    /// Builds a display for Spectre.Console Live (returns empty, we do our own rendering).
    /// </summary>
    public IRenderable BuildDisplay() => Text.Empty;

    /// <inheritdoc />
    public void OnWorkflowEvent(object? sender, WorkflowEvent evt)
    {
        lock (_lock)
        {
            _eventAggregator.HandleWorkflowEvent(evt);
        }
    }

    /// <inheritdoc />
    public void OnTaskEvent(object? sender, TaskEvent evt)
    {
        lock (_lock)
        {
            _eventAggregator.HandleTaskEvent(evt);
        }
    }

    /// <inheritdoc />
    public Task WaitForExitAsync()
    {
        var tcs = new TaskCompletionSource();
        var timer = new Timer(_ =>
        {
            lock (_lock) { if (_state.ExitRequested) tcs.TrySetResult(); }
        }, null, 50, 50);
        return tcs.Task.ContinueWith(_ => timer.Dispose());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _refreshTimer?.Dispose();
        _terminalProvider.ShowCursor();
    }

    private void AddWave(int index, IReadOnlyList<WorkflowTask> tasks, bool isAlways)
    {
        _state.Waves.Add(new WaveInfo(index, isAlways));
        foreach (var t in tasks)
            _state.Tasks.Add(new TaskInfo(t.Id, t.DisplayName, index));
    }

    private void RenderGraph(int width)
    {
        if (_workflow is null || _executionPlan is null) return;

        // Calculate required width based on actual task names and count
        var allTasks = _executionPlan.Waves
            .SelectMany(w => w.Tasks)
            .Concat(_executionPlan.AlwaysTasks)
            .ToList();

        var maxTasksInWave = _executionPlan.Waves
            .Select(w => w.Tasks.Count)
            .DefaultIfEmpty(1)
            .Max();

        // Box width is max label length + 4, minimum 10
        var maxLabelLen = allTasks.Count > 0
            ? allTasks.Max(t => t.Id.Length)
            : 10;
        var boxWidth = Math.Max(maxLabelLen + 4, 10);

        // Total width: (count * boxWidth) + ((count - 1) * spacing) + margin
        // BoxSpacing in AsciiGraphRenderer is 4
        const int boxSpacing = 4;
        var requiredWidth = maxTasksInWave * boxWidth + (maxTasksInWave - 1) * boxSpacing + 10;

        // Allow graph to be wider than terminal for horizontal scrolling
        var canvasWidth = Math.Max(width - 4, requiredWidth);

        var renderer = new AsciiGraphRenderer
        {
            UseTaskIds = true,
            CanvasWidth = Math.Max(60, canvasWidth)
        };
        var graphText = renderer.Render(_workflow, _executionPlan);
        var lines = graphText.Split('\n');
        _graphView.SetGraphLines(lines);
        _graphRenderedWidth = width;
    }

    private void Refresh()
    {
        lock (_lock)
        {
            _inputHandler.Process();
        }

        try { lock (_lock) { Render(); } }
        catch (InvalidOperationException ex)
        {
            // Render interrupted - log at debug level for diagnostics
            _logger.LogDebug(ex, "Render interrupted during refresh");
        }
    }

    private void Render()
    {
        var size = _terminalProvider.GetSize();
        if (size != _lastSize)
        {
            _lastSize = size;
            _terminalProvider.ClearScreen();

            if (_state.ShowingGraph && Math.Abs(size.Width - _graphRenderedWidth) > 10)
            {
                RenderGraph(size.Width);
            }
        }
        else
        {
            _terminalProvider.Home();
        }

        var lines = _state.ShowingGraph
            ? _graphView.Build(_state, size.Width, size.Height)
            : _state.InspectingTask != null
                ? _inspectorView.Build(_state, size.Width, size.Height)
                : _mainView.Build(_state, size.Width, size.Height);

        NormalizeLineCount(lines, size.Height);
        ToastOverlay.Apply(lines, _toasts, size.Width);
        RenderLines(lines);
    }

    private void RenderLines(List<string> lines)
    {
        if (_console is null) return;

        for (var i = 0; i < lines.Count; i++)
        {
            _console.Write(new Markup(lines[i]));
            _terminalProvider.ClearToEndOfLine();
            if (i < lines.Count - 1) System.Console.WriteLine();
        }
    }

    private static void NormalizeLineCount(List<string> lines, int targetHeight)
    {
        while (lines.Count < targetHeight) lines.Add("");
        if (lines.Count > targetHeight) lines.RemoveRange(targetHeight, lines.Count - targetHeight);
    }

    private void InspectSelectedTask()
    {
        var task = _state.Tasks[_state.SelectedIndex];
        if (task.Output.Count > 0)
        {
            _state.InspectingTask = task;
            var contentHeight = Math.Max(1, _terminalProvider.GetSize().Height - 3);
            _state.InspectScroll = Math.Max(0, task.Output.Count - contentHeight);
        }
    }

    private void CancelTask(TaskInfo task)
    {
        if (_workflowContext is null)
        {
            _logger.LogWarning("Cannot cancel task: workflow context not set");
            return;
        }

        if (task.Status != ExecutionStatus.Running)
        {
            _logger.LogDebug("Task '{TaskId}' is not running, cannot cancel", task.Id);
            return;
        }

        _logger.LogInformation("Requesting cancellation of task '{TaskId}'", task.Id);
        _workflowContext.RequestTaskCancellation(task.Id);
        _toasts.Show($"Cancelling task '{task.Name}'...", ToastType.Info);
    }

    /// <summary>
    /// Sets the task retrier for retry functionality.
    /// </summary>
    /// <param name="retrier">The task retrier.</param>
    public void SetTaskRetrier(ITaskRetrier retrier)
    {
        ArgumentNullException.ThrowIfNull(retrier);
        lock (_lock)
        {
            _taskRetrier = retrier;
        }
    }

    private void RetryTask(TaskInfo task)
    {
        if (_workflowContext is null)
        {
            _logger.LogWarning("Cannot retry task: workflow context not set");
            _toasts.Show("Cannot retry: no workflow context", ToastType.Error);
            return;
        }

        if (_taskRetrier is null)
        {
            _logger.LogWarning("Cannot retry task: task retrier not set");
            _toasts.Show("Retry not available", ToastType.Error);
            return;
        }

        if (task.Status != ExecutionStatus.Failed && task.Status != ExecutionStatus.TimedOut)
        {
            _logger.LogDebug("Task '{TaskId}' has not failed, cannot retry", task.Id);
            return;
        }

        _logger.LogInformation("Retrying task '{TaskId}'", task.Id);
        _toasts.Show($"Retrying task '{task.Name}'...", ToastType.Info);

        // Clear previous output
        task.Output.Clear();
        task.Status = ExecutionStatus.Pending;
        task.Duration = null;
        task.ExitCode = null;

        // Execute retry asynchronously with proper error handling
        ExecuteRetryAsync(task.Id, _workflowContext, _workflowContext.CancellationToken);
    }

    private async void ExecuteRetryAsync(string taskId, WorkflowContext context, CancellationToken cancellationToken)
    {
        try
        {
            await _taskRetrier!.RetryTaskAsync(taskId, context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Retry of task '{TaskId}' was cancelled", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry task '{TaskId}'", taskId);
            _toasts.Show($"Retry failed: {ex.Message}", ToastType.Error);
        }
    }
}
