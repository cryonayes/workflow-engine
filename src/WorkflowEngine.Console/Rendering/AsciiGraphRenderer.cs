using System.Text;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Renders workflow graphs in ASCII format with direct dependency arrows.
/// </summary>
public sealed class AsciiGraphRenderer
{
    private const int BoxSpacing = 4;
    private const int DefaultCanvasWidth = 100;

    /// <summary>
    /// Gets or sets whether to use task IDs instead of display names.
    /// </summary>
    public bool UseTaskIds { get; set; } = true;

    /// <summary>
    /// Gets or sets the canvas width. Defaults to 100.
    /// </summary>
    public int CanvasWidth { get; set; } = DefaultCanvasWidth;

    /// <summary>
    /// Renders the workflow as ASCII art.
    /// </summary>
    public string Render(Workflow workflow, ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(plan);

        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"  {workflow.Name}");
        sb.Append("  ");
        sb.Append('─', Math.Min(workflow.Name.Length, CanvasWidth - 4));
        sb.AppendLine();
        sb.AppendLine();

        if (plan.Waves.Count == 0 && plan.AlwaysTasks.Count == 0)
        {
            sb.AppendLine("  (no tasks)");
            return sb.ToString();
        }

        // Build task position info
        var taskWave = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var taskIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var w = 0; w < plan.Waves.Count; w++)
        {
            var tasks = plan.Waves[w].Tasks;
            for (var i = 0; i < tasks.Count; i++)
            {
                taskWave[tasks[i].Id] = w;
                taskIndex[tasks[i].Id] = i;
            }
        }

        // Calculate box width
        var allTasks = plan.Waves.SelectMany(w => w.Tasks).Concat(plan.AlwaysTasks).ToList();
        var maxLabelLen = allTasks.Max(t => GetLabel(t).Length);
        var boxWidth = Math.Max(maxLabelLen + 4, 10);

        // Build connection info - track which x-positions have lines passing through each wave
        var wavePassingLines = new Dictionary<int, HashSet<int>>(); // wave -> set of x positions with passing lines
        for (var w = 0; w < plan.Waves.Count; w++)
            wavePassingLines[w] = new HashSet<int>();

        // Calculate all connections and their x-positions
        var connections = new List<(string srcId, string tgtId, int srcWave, int tgtWave, int srcX, int tgtX)>();

        foreach (var wave in plan.Waves)
        {
            var wavePositions = CalculatePositions(wave.Tasks.Count, boxWidth, CanvasWidth);
            foreach (var task in wave.Tasks)
            {
                var tgtWave = taskWave[task.Id];
                var tgtIdx = taskIndex[task.Id];
                var tgtPositions = CalculatePositions(plan.Waves[tgtWave].Tasks.Count, boxWidth, CanvasWidth);
                var tgtX = tgtPositions[tgtIdx] + boxWidth / 2;

                foreach (var depId in task.DependsOn)
                {
                    if (!taskWave.TryGetValue(depId, out var srcWave)) continue;
                    if (!taskIndex.TryGetValue(depId, out var srcIdx)) continue;

                    var srcPositions = CalculatePositions(plan.Waves[srcWave].Tasks.Count, boxWidth, CanvasWidth);
                    var srcX = srcPositions[srcIdx] + boxWidth / 2;

                    connections.Add((depId, task.Id, srcWave, tgtWave, srcX, tgtX));

                    // Mark passing lines for intermediate waves
                    for (var w = srcWave + 1; w < tgtWave; w++)
                    {
                        wavePassingLines[w].Add(srcX);
                    }
                }
            }
        }

        // Render waves
        for (var waveIdx = 0; waveIdx < plan.Waves.Count; waveIdx++)
        {
            var wave = plan.Waves[waveIdx];
            var positions = CalculatePositions(wave.Tasks.Count, boxWidth, CanvasWidth);

            // Get lines passing through this wave (from earlier connections)
            var passingLines = wavePassingLines[waveIdx];

            // Get connections ending at this wave
            var incomingConns = connections.Where(c => c.tgtWave == waveIdx).ToList();

            // Draw incoming arrows if any
            if (incomingConns.Count > 0 || passingLines.Count > 0)
            {
                // Vertical lines coming down
                var line = CreateLine(CanvasWidth);
                foreach (var x in passingLines)
                    SetChar(line, x, '│');
                foreach (var conn in incomingConns)
                    SetChar(line, conn.srcX, '│');
                sb.Append("  ");
                sb.AppendLine(new string(line).TrimEnd());

                // Horizontal routing if needed
                var needsRouting = incomingConns.Any(c => c.srcX != c.tgtX);
                if (needsRouting)
                {
                    var routeLine = CreateLine(CanvasWidth);

                    // Continue passing lines
                    foreach (var x in passingLines)
                        SetChar(routeLine, x, '│');

                    // Group connections by target
                    var byTarget = incomingConns.GroupBy(c => c.tgtX).ToList();
                    foreach (var group in byTarget)
                    {
                        var tgtX = group.Key;
                        var sources = group.Select(c => c.srcX).Distinct().OrderBy(x => x).ToList();

                        if (sources.Count == 1 && sources[0] == tgtX)
                            continue; // straight line, no routing needed

                        var minX = Math.Min(sources.Min(), tgtX);
                        var maxX = Math.Max(sources.Max(), tgtX);

                        for (var x = minX; x <= maxX; x++)
                        {
                            if (x < 0 || x >= CanvasWidth) continue;

                            var isSource = sources.Contains(x);
                            var isTarget = x == tgtX;
                            var isPassing = passingLines.Contains(x);

                            var cur = routeLine[x];
                            var newChar = GetRoutingChar(isSource, isTarget, isPassing, x > minX, x < maxX, cur);
                            SetChar(routeLine, x, newChar);
                        }
                    }

                    sb.Append("  ");
                    sb.AppendLine(new string(routeLine).TrimEnd());

                    // Vertical lines to targets
                    var downLine = CreateLine(CanvasWidth);
                    foreach (var x in passingLines)
                        SetChar(downLine, x, '│');
                    foreach (var conn in incomingConns)
                        SetChar(downLine, conn.tgtX, '│');
                    sb.Append("  ");
                    sb.AppendLine(new string(downLine).TrimEnd());
                }

                // Arrow heads
                var arrowLine = CreateLine(CanvasWidth);
                foreach (var x in passingLines)
                    SetChar(arrowLine, x, '│');
                var targetXs = incomingConns.Select(c => c.tgtX).Distinct();
                foreach (var x in targetXs)
                    SetChar(arrowLine, x, '▼');
                sb.Append("  ");
                sb.AppendLine(new string(arrowLine).TrimEnd());
            }

            // Task boxes
            RenderTaskBoxes(sb, wave.Tasks.ToList(), boxWidth, passingLines);

            // Get connections starting from this wave
            var outgoingConns = connections.Where(c => c.srcWave == waveIdx).ToList();

            // Draw outgoing lines
            if (outgoingConns.Count > 0 || passingLines.Count > 0)
            {
                var outLine = CreateLine(CanvasWidth);
                foreach (var x in passingLines)
                    SetChar(outLine, x, '│');
                foreach (var conn in outgoingConns)
                    SetChar(outLine, conn.srcX, '│');
                sb.Append("  ");
                sb.AppendLine(new string(outLine).TrimEnd());
            }
        }

        sb.AppendLine();

        // Always tasks
        if (plan.AlwaysTasks.Count > 0)
        {
            sb.AppendLine("  Always:");
            RenderTaskBoxes(sb, plan.AlwaysTasks.ToList(), boxWidth, new HashSet<int>());
        }

        return sb.ToString();
    }

    private string GetLabel(WorkflowTask task) => UseTaskIds ? task.Id : task.DisplayName;

    private void RenderTaskBoxes(StringBuilder sb, List<WorkflowTask> tasks, int boxWidth, HashSet<int> passingLines)
    {
        if (tasks.Count == 0) return;

        var positions = CalculatePositions(tasks.Count, boxWidth, CanvasWidth);

        // Top border
        var line1 = CreateLine(CanvasWidth);
        foreach (var x in passingLines)
            SetChar(line1, x, '│');
        foreach (var (task, i) in tasks.Select((t, i) => (t, i)))
        {
            var pos = positions[i];
            SetChar(line1, pos, '┌');
            for (var j = 1; j < boxWidth - 1; j++)
                SetChar(line1, pos + j, '─');
            SetChar(line1, pos + boxWidth - 1, '┐');
        }
        sb.Append("  ");
        sb.AppendLine(new string(line1).TrimEnd());

        // Middle with label
        var line2 = CreateLine(CanvasWidth);
        foreach (var x in passingLines)
            SetChar(line2, x, '│');
        foreach (var (task, i) in tasks.Select((t, i) => (t, i)))
        {
            var pos = positions[i];
            var label = GetLabel(task);
            var truncated = label.Length > boxWidth - 4 ? label[..(boxWidth - 5)] + "…" : label;

            SetChar(line2, pos, '│');
            var textStart = pos + 1 + (boxWidth - 2 - truncated.Length) / 2;
            for (var j = 0; j < truncated.Length; j++)
                SetChar(line2, textStart + j, truncated[j]);
            SetChar(line2, pos + boxWidth - 1, '│');
        }
        sb.Append("  ");
        sb.AppendLine(new string(line2).TrimEnd());

        // Bottom border
        var line3 = CreateLine(CanvasWidth);
        foreach (var x in passingLines)
            SetChar(line3, x, '│');
        foreach (var (task, i) in tasks.Select((t, i) => (t, i)))
        {
            var pos = positions[i];
            SetChar(line3, pos, '└');
            for (var j = 1; j < boxWidth - 1; j++)
                SetChar(line3, pos + j, '─');
            SetChar(line3, pos + boxWidth - 1, '┘');
        }
        sb.Append("  ");
        sb.AppendLine(new string(line3).TrimEnd());
    }

    private static char GetRoutingChar(bool isSource, bool isTarget, bool isPassing, bool hasLeft, bool hasRight, char existing)
    {
        var up = isSource || isPassing;
        var down = isTarget || isPassing;
        var left = hasLeft;
        var right = hasRight;

        // Merge with existing character
        if (existing == '│') { up = true; down = true; }
        if (existing == '─') { left = true; right = true; }
        if (existing == '┼') { up = true; down = true; left = true; right = true; }

        return (up, down, left, right) switch
        {
            (true, true, true, true) => '┼',
            (true, true, true, false) => '┤',
            (true, true, false, true) => '├',
            (true, true, false, false) => '│',
            (true, false, true, true) => '┴',
            (true, false, true, false) => '┘',
            (true, false, false, true) => '└',
            (true, false, false, false) => '│',
            (false, true, true, true) => '┬',
            (false, true, true, false) => '┐',
            (false, true, false, true) => '┌',
            (false, true, false, false) => '│',
            (false, false, true, true) => '─',
            (false, false, true, false) => '─',
            (false, false, false, true) => '─',
            _ => ' '
        };
    }

    private static int[] CalculatePositions(int count, int boxWidth, int canvasWidth)
    {
        var positions = new int[count];
        var totalWidth = count * boxWidth + (count - 1) * BoxSpacing;
        var startX = Math.Max(0, (canvasWidth - totalWidth) / 2);

        for (var i = 0; i < count; i++)
            positions[i] = startX + i * (boxWidth + BoxSpacing);

        return positions;
    }

    private static char[] CreateLine(int width)
    {
        var line = new char[width];
        Array.Fill(line, ' ');
        return line;
    }

    private static void SetChar(char[] line, int pos, char c)
    {
        if (pos >= 0 && pos < line.Length)
            line[pos] = c;
    }
}
