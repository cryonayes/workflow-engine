using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Represents visual style for rendering (icon, color, text).
/// </summary>
internal readonly record struct TaskStyle(string Icon, string Color, string Text = "")
{
    /// <summary>
    /// Gets the style for workflow status display.
    /// </summary>
    public static TaskStyle ForWorkflow(bool running, bool failed, bool paused = false, bool cancelled = false) =>
        paused ? new("◉", "cyan") :
        running ? new("●", "yellow") :
        failed ? new("✗", "red") :
        cancelled ? new("⊘", "yellow") : new("✓", "green");

    /// <summary>
    /// Gets the style for wave status display.
    /// </summary>
    public static TaskStyle ForWave(WaveStatus status) => status switch
    {
        WaveStatus.Pending => new("", "grey", "pending"),
        WaveStatus.Running => new("", "yellow", "running"),
        WaveStatus.Completed => new("", "green", "done"),
        _ => new("", "grey", "")
    };

    /// <summary>
    /// Gets the style for task execution status display.
    /// </summary>
    public static TaskStyle ForTask(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Pending => new("○", "grey"),
        ExecutionStatus.Running => new("●", "yellow"),
        ExecutionStatus.Succeeded => new("✓", "green"),
        ExecutionStatus.Failed => new("✗", "red"),
        ExecutionStatus.Skipped => new("⊖", "blue"),
        ExecutionStatus.Cancelled => new("⊘", "orange1"),
        ExecutionStatus.TimedOut => new("⏱", "red"),
        _ => new("?", "grey")
    };

    /// <summary>
    /// Gets just the color for a task execution status.
    /// </summary>
    public static string GetColorForStatus(ExecutionStatus status) => ForTask(status).Color;
}
