using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Renders the main task list view with scrolling support.
/// </summary>
internal sealed class MainViewRenderer : IViewRenderer
{
    /// <inheritdoc />
    public List<string> Build(RendererState state, int width, int height)
    {
        var contentWidth = Math.Max(LayoutConstants.MinWidth, width - 2);
        var contentHeight = Math.Max(1, height - 5); // 3 header + 2 footer

        // Build content and track task positions
        var (contentLines, taskLineIndices) = BuildContentLines(state, contentWidth);

        // Calculate and apply scroll bounds
        var (vScroll, maxVScroll) = CalculateVerticalScroll(state, contentLines.Count, contentHeight, taskLineIndices);
        var (hScroll, maxHScroll) = CalculateHorizontalScroll(state, contentLines, contentWidth);

        // Build view
        var lines = new List<string>
        {
            BuildHeader(state, contentWidth),
            BuildProgressBar(state, contentWidth),
            ""
        };

        AddScrollableContent(lines, contentLines, vScroll, hScroll, contentHeight, contentWidth);
        RenderHelpers.PadLines(lines, contentHeight - Math.Min(contentLines.Count - vScroll, contentHeight));
        AddFooter(lines, state, contentWidth, vScroll, contentHeight, contentLines.Count, maxVScroll, maxHScroll, hScroll);

        return lines;
    }

    private static (int vScroll, int maxVScroll) CalculateVerticalScroll(
        RendererState state,
        int totalLines,
        int contentHeight,
        List<int> taskLineIndices)
    {
        var maxVScroll = Math.Max(0, totalLines - contentHeight);
        var vScroll = Math.Clamp(state.MainScroll, 0, maxVScroll);

        // Auto-scroll to keep selected task visible with context
        if (state.SelectedIndex >= 0 && state.SelectedIndex < taskLineIndices.Count)
        {
            var selectedLine = taskLineIndices[state.SelectedIndex];
            var contextLine = Math.Max(0, selectedLine - 1); // Keep wave header visible

            if (contextLine < vScroll)
                vScroll = contextLine;
            else if (selectedLine >= vScroll + contentHeight)
                vScroll = Math.Min(maxVScroll, selectedLine - contentHeight + 1);
        }

        vScroll = Math.Clamp(vScroll, 0, maxVScroll);
        state.MainScroll = vScroll;

        return (vScroll, maxVScroll);
    }

    private static (int hScroll, int maxHScroll) CalculateHorizontalScroll(
        RendererState state,
        List<string> contentLines,
        int contentWidth)
    {
        var maxLineWidth = contentLines.Count > 0 ? contentLines.Max(RenderHelpers.GetVisualLength) : 0;
        var maxHScroll = Math.Max(0, maxLineWidth - contentWidth);
        var hScroll = Math.Clamp(state.MainHorizontalScroll, 0, maxHScroll);
        state.MainHorizontalScroll = hScroll;

        return (hScroll, maxHScroll);
    }

    private static (List<string> Lines, List<int> TaskLineIndices) BuildContentLines(RendererState state, int contentWidth)
    {
        var lines = new List<string>();
        var taskLineIndices = new List<int>();
        var taskIndex = 0;

        for (var i = 0; i < state.Waves.Count; i++)
        {
            var wave = state.Waves[i];
            var waveTasks = state.Tasks.Where(t => t.WaveIndex == wave.Index).ToList();

            lines.Add(BuildWaveHeader(wave, waveTasks, contentWidth));

            foreach (var task in waveTasks)
            {
                taskLineIndices.Add(lines.Count);
                lines.Add(BuildTaskRow(task, taskIndex == state.SelectedIndex, contentWidth));
                taskIndex++;
            }

            if (i < state.Waves.Count - 1)
                lines.Add("");
        }

        return (lines, taskLineIndices);
    }

    private static void AddScrollableContent(
        List<string> lines,
        List<string> contentLines,
        int vScroll,
        int hScroll,
        int contentHeight,
        int contentWidth)
    {
        var visibleLines = contentLines.Skip(vScroll).Take(contentHeight);
        foreach (var line in visibleLines)
        {
            lines.Add(hScroll > 0 ? RenderHelpers.SliceMarkupLine(line, hScroll, contentWidth) : line);
        }
    }

    private static string BuildHeader(RendererState state, int width)
    {
        var elapsed = state.FinalDuration ?? (DateTimeOffset.UtcNow - state.StartTime);
        var pct = CalculatePercentage(state.CompletedTasks, state.TotalTasks);
        var style = TaskStyle.ForWorkflow(state.IsRunning, state.FailedTasks > 0, state.IsPaused, state.CancelledTasks > 0);
        var eta = state.IsRunning && state.CompletedTasks > 0 && !state.IsPaused ? FormatEta(state) : "";
        var stepIndicator = state.StepMode ? " [dim cyan]STEP[/]" : "";

        return $" [{style.Color}]{style.Icon}[/] [bold]{RenderHelpers.Escape(state.WorkflowName)}[/]{stepIndicator} " +
               $"[grey]({state.CompletedTasks}/{state.TotalTasks}) {pct}% {TextFormatter.FormatDuration(elapsed)}{eta}[/]";
    }

    private static string BuildProgressBar(RendererState state, int width)
    {
        var pct = CalculatePercentage(state.CompletedTasks, state.TotalTasks);
        return " " + ProgressBar.Build(pct, Math.Max(10, width - 4), '━', '─', "green", "grey");
    }

    private static string BuildWaveHeader(WaveInfo wave, List<TaskInfo> tasks, int width)
    {
        var label = wave.IsAlways ? "Always" : $"Wave {wave.Index + 1}";
        var style = TaskStyle.ForWave(wave.Status);
        var prefix = $" [bold {style.Color}]{label}[/] [dim][[{style.Text}]][/]";

        if (wave.Status != WaveStatus.Running || tasks.Count == 0)
            return prefix;

        var done = tasks.Count(t => t.Status >= ExecutionStatus.Succeeded);
        var pct = done * 100 / tasks.Count;
        var nameWidth = Math.Max(10, (width - 30) / 2);
        var barWidth = Math.Max(8, 7 + nameWidth + 14 - label.Length - 12 - $"{done}/{tasks.Count}".Length - 3);

        return $"{prefix}  {ProgressBar.Build(pct, barWidth, '█', '░', "green", "grey")} {done}/{tasks.Count}";
    }

    private static string BuildTaskRow(TaskInfo task, bool selected, int width)
    {
        var style = TaskStyle.ForTask(task.Status);
        var nameWidth = Math.Max(10, (width - 30) / 2);
        var name = RenderHelpers.Truncate(task.Name, nameWidth).PadRight(nameWidth);
        var outputCount = task.Output.Count > 0 ? $"({task.Output.Count})".PadRight(6) : "      ";
        var duration = FormatTaskDuration(task);
        var durationColor = task.Status == ExecutionStatus.Running ? "yellow" : "grey";
        var background = selected ? " on grey23" : "";
        var cursor = selected ? "[yellow]▸[/]" : " ";

        return $"[{style.Color}{background}]   {cursor} {style.Icon} {RenderHelpers.Escape(name)}[/]" +
               $"[dim]{outputCount}[/][{durationColor}]{duration}[/]";
    }

    private static string FormatTaskDuration(TaskInfo task)
    {
        if (task.Duration.HasValue)
            return TextFormatter.FormatDuration(task.Duration.Value).PadLeft(8);
        if (task.Status == ExecutionStatus.Running)
            return TextFormatter.FormatDuration(DateTimeOffset.UtcNow - task.StartTime).PadLeft(8);
        return "        ";
    }

    private static void AddFooter(
        List<string> lines,
        RendererState state,
        int width,
        int vScroll,
        int contentHeight,
        int totalLines,
        int maxVScroll,
        int maxHScroll,
        int hScroll)
    {
        var badge = GetStatusBadge(state);
        var failed = state.FailedTasks > 0 ? $" [red]{state.FailedTasks} failed[/]" : "";
        var cancelled = state.CancelledTasks > 0 ? $" [yellow]{state.CancelledTasks} cancelled[/]" : "";
        var hasScroll = maxVScroll > 0 || maxHScroll > 0;

        var scrollInfo = "";
        if (hasScroll)
        {
            var posText = RenderHelpers.BuildScrollPositionText(vScroll, contentHeight, totalLines);
            var scrollPct = maxVScroll > 0 ? vScroll * 100 / maxVScroll : 100;
            var scrollBar = RenderHelpers.BuildScrollIndicator(scrollPct);
            var hScrollText = maxHScroll > 0 ? $" col:{hScroll + 1}" : "";
            scrollInfo = $"  [grey]{posText} {scrollBar}{hScrollText}[/]";
        }

        var help = GetHelpText(state, hasScroll);
        lines.Add(RenderHelpers.HorizontalRule(width));
        lines.Add($" {badge}{failed}{cancelled}{scrollInfo}  {help}");
    }

    private static string GetStatusBadge(RendererState state) => (state.IsWaitingToStart, state.IsPaused, state.IsRunning, state.StepMode, state.FailedTasks > 0, state.CancelledTasks > 0) switch
    {
        (true, _, _, _, _, _) => "[black on cyan] READY [/]",
        (_, true, _, _, _, _) => "[black on cyan] PAUSED [/]",
        (_, _, true, true, _, _) => "[black on yellow] STEPPING [/]",
        (_, _, true, false, _, _) => "[black on yellow] RUNNING [/]",
        (_, _, false, _, true, _) => "[white on red] FAILED [/]",
        (_, _, false, _, false, true) => "[black on yellow] CANCELLED [/]",
        _ => "[black on green] DONE [/]"
    };

    private static string GetHelpText(RendererState state, bool hasScroll) => (state.IsWaitingToStart, state.IsPaused, state.IsRunning) switch
    {
        (true, _, _) => "[cyan]Space[/][grey] start  ↑↓ select  g graph  e export[/]",
        (_, true, _) => $"[cyan]Space[/][grey] next  Enter inspect  ↑↓ select{(hasScroll ? " ←→" : "")}  c cancel  g graph  e export[/]",
        (_, _, true) => $"[grey]↑↓ select{(hasScroll ? " ←→" : "")}  Enter inspect  c cancel  g graph  e export[/]",
        _ => $"[grey]↑↓ select{(hasScroll ? " ←→" : "")}  Enter inspect  g graph  e export  q quit[/]"
    };

    private static string FormatEta(RendererState state)
    {
        var elapsed = (DateTimeOffset.UtcNow - state.StartTime).TotalSeconds;
        var remaining = (elapsed / state.CompletedTasks) * (state.TotalTasks - state.CompletedTasks);
        return remaining < 1 ? "" : $" [dim]eta {TextFormatter.FormatDuration(TimeSpan.FromSeconds(remaining))}[/]";
    }

    private static int CalculatePercentage(int done, int total) => total > 0 ? done * 100 / total : 0;
}
