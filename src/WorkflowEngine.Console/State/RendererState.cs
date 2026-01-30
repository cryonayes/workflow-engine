namespace WorkflowEngine.Console.State;

/// <summary>
/// Mutable state for the progress renderer.
/// </summary>
internal sealed class RendererState
{
    /// <summary>
    /// Gets or sets the workflow name.
    /// </summary>
    public string WorkflowName { get; set; } = "";

    /// <summary>
    /// Gets or sets the total number of tasks.
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of completed tasks.
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of failed tasks.
    /// </summary>
    public int FailedTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of cancelled tasks.
    /// </summary>
    public int CancelledTasks { get; set; }

    /// <summary>
    /// Gets or sets when the workflow started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the final duration when workflow completes.
    /// </summary>
    public TimeSpan? FinalDuration { get; set; }

    /// <summary>
    /// Gets or sets whether the workflow is still running.
    /// </summary>
    public bool IsRunning { get; set; } = true;

    /// <summary>
    /// Gets or sets whether exit has been requested.
    /// </summary>
    public bool ExitRequested { get; set; }

    /// <summary>
    /// Gets or sets the currently selected task index.
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// Gets the list of waves.
    /// </summary>
    public List<WaveInfo> Waves { get; } = [];

    /// <summary>
    /// Gets the list of tasks.
    /// </summary>
    public List<TaskInfo> Tasks { get; } = [];

    /// <summary>
    /// Gets or sets the task being inspected.
    /// </summary>
    public TaskInfo? InspectingTask { get; set; }

    /// <summary>
    /// Gets or sets the vertical scroll position in inspector view.
    /// </summary>
    public int InspectScroll { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scroll position in inspector view.
    /// </summary>
    public int InspectHorizontalScroll { get; set; }

    /// <summary>
    /// Gets or sets whether step mode is enabled.
    /// </summary>
    public bool StepMode { get; set; }

    /// <summary>
    /// Gets or sets whether execution is paused.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets whether waiting to start the first task.
    /// </summary>
    public bool IsWaitingToStart { get; set; }

    /// <summary>
    /// Gets or sets whether the graph view is shown.
    /// </summary>
    public bool ShowingGraph { get; set; }

    /// <summary>
    /// Gets or sets the vertical scroll position in graph view.
    /// </summary>
    public int GraphScroll { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scroll position in graph view.
    /// </summary>
    public int GraphHorizontalScroll { get; set; }

    /// <summary>
    /// Gets or sets the vertical scroll position in main view.
    /// </summary>
    public int MainScroll { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scroll position in main view.
    /// </summary>
    public int MainHorizontalScroll { get; set; }
}
