using System.Text;
using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Renders the dependency graph view with horizontal and vertical scrolling.
/// </summary>
internal sealed class GraphViewRenderer : IViewRenderer
{
    private string[] _graphLines = [];
    private int _maxLineWidth;

    /// <summary>
    /// Sets the graph lines to render.
    /// </summary>
    public void SetGraphLines(string[] lines)
    {
        _graphLines = lines;
        _maxLineWidth = lines.Length > 0 ? lines.Max(l => l.Length) : 0;
    }

    /// <summary>
    /// Gets the total number of graph lines.
    /// </summary>
    public int LineCount => _graphLines.Length;

    /// <summary>
    /// Gets the maximum line width for horizontal scrolling.
    /// </summary>
    public int MaxLineWidth => _maxLineWidth;

    /// <inheritdoc />
    public List<string> Build(RendererState state, int width, int height)
    {
        var contentWidth = width - 2;
        var contentHeight = Math.Max(1, height - 3); // 1 header + 2 footer

        // Calculate scroll bounds
        var maxVScroll = Math.Max(0, _graphLines.Length - contentHeight);
        var vScroll = Math.Clamp(state.GraphScroll, 0, maxVScroll);

        var maxHScroll = Math.Max(0, _maxLineWidth - contentWidth);
        var hScroll = Math.Clamp(state.GraphHorizontalScroll, 0, maxHScroll);

        // Update state with clamped values
        state.GraphScroll = vScroll;
        state.GraphHorizontalScroll = hScroll;

        // Build view
        var lines = new List<string>
        {
            BuildHeader(maxHScroll, hScroll)
        };

        AddContent(lines, state.Tasks, vScroll, hScroll, contentHeight, contentWidth);
        lines.Add(RenderHelpers.HorizontalRule(width));
        lines.Add(BuildFooter(vScroll, contentHeight, maxVScroll, maxHScroll, state.IsPaused));

        return lines;
    }

    private string BuildHeader(int maxHScroll, int hScroll)
    {
        var hScrollText = maxHScroll > 0
            ? $"  [grey]{RenderHelpers.BuildHorizontalScrollText(hScroll, _maxLineWidth)}[/]"
            : "";
        return $"[bold cyan]Dependency Graph[/]  [grey]({_graphLines.Length} lines)[/]{hScrollText}";
    }

    private void AddContent(List<string> lines, List<TaskInfo> tasks, int vScroll, int hScroll, int contentHeight, int contentWidth)
    {
        var taskColors = BuildTaskColorLookup(tasks);
        var visibleLines = _graphLines.Skip(vScroll).Take(contentHeight).ToList();

        foreach (var line in visibleLines)
        {
            var sliced = RenderHelpers.SliceLine(line, hScroll, contentWidth);
            lines.Add(ColorizeLine(sliced, taskColors));
        }

        RenderHelpers.PadLines(lines, contentHeight - visibleLines.Count);
    }

    private string BuildFooter(int vScroll, int contentHeight, int maxVScroll, int maxHScroll, bool isPaused)
    {
        var posText = RenderHelpers.BuildScrollPositionText(vScroll, contentHeight, _graphLines.Length);
        var scrollPct = maxVScroll > 0 ? vScroll * 100 / maxVScroll : 100;
        var scrollBar = RenderHelpers.BuildScrollIndicator(scrollPct);
        var stepHint = isPaused ? "[cyan]Space[/][grey] next  [/]" : "";
        var hScrollHint = maxHScroll > 0 ? "←→ scroll  " : "";

        return $"[grey]{posText}  {scrollBar}  {stepHint}{hScrollHint}[grey]↑↓ PgUp/Dn  g/Esc close[/]";
    }

    private static Dictionary<string, string> BuildTaskColorLookup(List<TaskInfo> tasks)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in tasks)
        {
            lookup[task.Id] = TaskStyle.GetColorForStatus(task.Status);
        }
        return lookup;
    }

    private static string ColorizeLine(string line, Dictionary<string, string> taskColors)
    {
        var replacements = FindTaskReplacements(line, taskColors);

        if (replacements.Count == 0)
            return $"[white]{RenderHelpers.Escape(line)}[/]";

        return BuildColorizedLine(line, replacements);
    }

    private static List<(int Start, int Length, string TaskId, string Color)> FindTaskReplacements(
        string line,
        Dictionary<string, string> taskColors)
    {
        var replacements = new List<(int Start, int Length, string TaskId, string Color)>();

        foreach (var (taskId, color) in taskColors)
        {
            var index = 0;
            while ((index = line.IndexOf(taskId, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                var before = index > 0 ? line[index - 1] : ' ';
                var after = index + taskId.Length < line.Length ? line[index + taskId.Length] : ' ';

                if (IsBoxBoundary(before) && IsBoxBoundary(after))
                {
                    replacements.Add((index, taskId.Length, line.Substring(index, taskId.Length), color));
                }
                index += taskId.Length;
            }
        }

        return FilterOverlappingReplacements(replacements);
    }

    private static List<(int Start, int Length, string TaskId, string Color)> FilterOverlappingReplacements(
        List<(int Start, int Length, string TaskId, string Color)> replacements)
    {
        var sorted = replacements
            .OrderBy(r => r.Start)
            .ThenByDescending(r => r.Length)
            .ToList();

        var filtered = new List<(int Start, int Length, string TaskId, string Color)>();
        var lastEnd = -1;

        foreach (var r in sorted)
        {
            if (r.Start >= lastEnd)
            {
                filtered.Add(r);
                lastEnd = r.Start + r.Length;
            }
        }

        return filtered;
    }

    private static string BuildColorizedLine(
        string line,
        List<(int Start, int Length, string TaskId, string Color)> replacements)
    {
        var result = new StringBuilder();
        var pos = 0;

        foreach (var (start, length, taskId, color) in replacements)
        {
            if (start > pos)
            {
                result.Append($"[white]{RenderHelpers.Escape(line[pos..start])}[/]");
            }
            result.Append($"[{color}]{RenderHelpers.Escape(taskId)}[/]");
            pos = start + length;
        }

        if (pos < line.Length)
        {
            result.Append($"[white]{RenderHelpers.Escape(line[pos..])}[/]");
        }

        return result.ToString();
    }

    private static bool IsBoxBoundary(char c) =>
        c == ' ' || c == '│' || c == '║' || c == '|';
}
