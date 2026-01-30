using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Renders the task output inspector view with horizontal and vertical scrolling.
/// </summary>
internal sealed class InspectorViewRenderer : IViewRenderer
{
    /// <inheritdoc />
    public List<string> Build(RendererState state, int width, int height)
    {
        var task = state.InspectingTask!;
        var contentWidth = width - 1;
        var contentHeight = Math.Max(1, height - 3); // 1 header + 2 footer

        // Calculate scroll bounds
        var maxVScroll = Math.Max(0, task.Output.Count - contentHeight);
        var vScroll = Math.Clamp(state.InspectScroll, 0, maxVScroll);

        var maxLineWidth = task.Output.Count > 0 ? task.Output.Max(o => o.Text.Length) : 0;
        var maxHScroll = Math.Max(0, maxLineWidth - contentWidth);
        var hScroll = Math.Clamp(state.InspectHorizontalScroll, 0, maxHScroll);

        // Update state with clamped values
        state.InspectScroll = vScroll;
        state.InspectHorizontalScroll = hScroll;

        // Build view
        var lines = new List<string>
        {
            BuildHeader(task, width, maxHScroll, hScroll, maxLineWidth)
        };

        AddContent(lines, task, vScroll, hScroll, contentHeight, contentWidth);
        lines.Add(RenderHelpers.HorizontalRule(width));
        lines.Add(BuildFooter(vScroll, contentHeight, task.Output.Count, maxVScroll, maxHScroll, state.IsPaused, task));

        return lines;
    }

    private static string BuildHeader(TaskInfo task, int width, int maxHScroll, int hScroll, int maxLineWidth)
    {
        var style = TaskStyle.ForTask(task.Status);
        var meta = BuildTaskMeta(task);
        var hScrollText = maxHScroll > 0
            ? $"  [grey]{RenderHelpers.BuildHorizontalScrollText(hScroll, maxLineWidth)}[/]"
            : "";
        var ruleWidth = Math.Max(0, width - task.Name.Length - meta.Length - 8 - (maxHScroll > 0 ? 20 : 0));

        return $"[{style.Color}]──[/] [{style.Color}]{style.Icon} {RenderHelpers.Escape(task.Name)}[/]  " +
               $"[grey]{meta}[/]{hScrollText}[{style.Color}]{new string('─', ruleWidth)}[/]";
    }

    private static string BuildTaskMeta(TaskInfo task)
    {
        var parts = new List<string>();
        if (task.Duration.HasValue) parts.Add(TextFormatter.FormatDuration(task.Duration.Value));
        if (task.ExitCode.HasValue) parts.Add($"exit {task.ExitCode}");
        return string.Join("  ", parts);
    }

    private static void AddContent(List<string> lines, TaskInfo task, int vScroll, int hScroll, int contentHeight, int contentWidth)
    {
        if (task.Output.Count == 0)
        {
            lines.Add("[grey](no output)[/]");
            RenderHelpers.PadLines(lines, contentHeight - 1);
            return;
        }

        var visible = task.Output.Skip(vScroll).Take(contentHeight).ToList();
        foreach (var output in visible)
        {
            var color = output.StreamType switch
            {
                OutputStreamType.Command => "cyan",
                OutputStreamType.StdErr => "red",
                _ => "white"
            };
            var sliced = RenderHelpers.SliceLine(output.Text, hScroll, contentWidth);
            lines.Add($"[{color}]{RenderHelpers.Escape(sliced)}[/]");
        }

        RenderHelpers.PadLines(lines, contentHeight - visible.Count);
    }

    private static string BuildFooter(int vScroll, int contentHeight, int totalLines, int maxVScroll, int maxHScroll, bool isPaused, TaskInfo task)
    {
        var posText = RenderHelpers.BuildScrollPositionText(vScroll, contentHeight, totalLines);
        var scrollPct = maxVScroll > 0 ? vScroll * 100 / maxVScroll : 100;
        var scrollBar = RenderHelpers.BuildScrollIndicator(scrollPct);
        var stepHint = isPaused ? "[cyan]Space[/][grey] next  [/]" : "";
        var cancelHint = task.Status == ExecutionStatus.Running ? "c cancel  " : "";
        var retryHint = task.Status == ExecutionStatus.Failed || task.Status == ExecutionStatus.TimedOut ? "r retry  " : "";
        var hScrollHint = maxHScroll > 0 ? "←→ " : "";

        return $"[grey]{posText}  {scrollBar}  {stepHint}[grey]{hScrollHint}↑↓ PgUp/Dn  {cancelHint}{retryHint}e export  Esc[/]";
    }
}
