using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Provides consistent color mappings for execution statuses.
/// </summary>
public static class StatusColorProvider
{
    /// <summary>
    /// Gets the Spectre.Console color name for an execution status.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <returns>The color name.</returns>
    public static string GetColor(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "green",
        ExecutionStatus.Failed => "red",
        ExecutionStatus.TimedOut => "red",
        ExecutionStatus.Cancelled => "yellow",
        ExecutionStatus.Running => "blue",
        ExecutionStatus.Pending => "grey",
        ExecutionStatus.Skipped => "dim",
        _ => "white"
    };

    /// <summary>
    /// Gets the display label for an execution status.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <returns>The display label.</returns>
    public static string GetLabel(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "SUCCESS",
        ExecutionStatus.Failed => "FAILED",
        ExecutionStatus.TimedOut => "TIMED OUT",
        ExecutionStatus.Cancelled => "CANCELLED",
        ExecutionStatus.Running => "RUNNING",
        ExecutionStatus.Pending => "PENDING",
        ExecutionStatus.Skipped => "SKIPPED",
        _ => status.ToString().ToUpperInvariant()
    };

    /// <summary>
    /// Gets formatted status text with color markup.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <returns>The colored status text.</returns>
    public static string GetColoredLabel(ExecutionStatus status)
    {
        var color = GetColor(status);
        var label = GetLabel(status);
        return $"[{color}]{label}[/]";
    }

    /// <summary>
    /// Gets the emoji/symbol for an execution status.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <returns>The status symbol.</returns>
    public static string GetSymbol(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "✓",
        ExecutionStatus.Failed => "✗",
        ExecutionStatus.TimedOut => "⏱",
        ExecutionStatus.Cancelled => "⊘",
        ExecutionStatus.Running => "●",
        ExecutionStatus.Pending => "○",
        ExecutionStatus.Skipped => "⊖",
        _ => "?"
    };
}
