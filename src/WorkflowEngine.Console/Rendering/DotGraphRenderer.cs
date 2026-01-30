using System.Text;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Renders workflow graphs in Graphviz DOT format.
/// </summary>
public sealed class DotGraphRenderer
{
    /// <summary>
    /// Renders the workflow as a DOT graph.
    /// </summary>
    public string Render(Workflow workflow, ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(plan);

        var sb = new StringBuilder();

        var escapedName = EscapeString(workflow.Name);
        sb.AppendLine($"digraph \"{escapedName}\" {{");
        sb.AppendLine("    rankdir=TB;");
        sb.AppendLine("    node [shape=box, style=rounded];");
        sb.AppendLine();

        foreach (var wave in plan.Waves)
        {
            sb.AppendLine($"    subgraph cluster_wave{wave.WaveIndex} {{");
            sb.AppendLine($"        label=\"Wave {wave.WaveIndex}\";");
            sb.AppendLine("        style=dashed;");
            sb.AppendLine("        color=gray;");
            sb.Append("        ");
            sb.AppendLine(string.Join("; ", wave.Tasks.Select(t => EscapeId(t.Id))) + ";");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        if (plan.AlwaysTasks.Count > 0)
        {
            sb.AppendLine("    subgraph cluster_always {");
            sb.AppendLine("        label=\"Always\";");
            sb.AppendLine("        style=dashed;");
            sb.AppendLine("        color=gray;");
            sb.Append("        ");
            sb.AppendLine(string.Join("; ", plan.AlwaysTasks.Select(t => EscapeId(t.Id))) + ";");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        foreach (var task in workflow.Tasks)
        {
            if (task.Name != null && task.Name != task.Id)
                sb.AppendLine($"    {EscapeId(task.Id)} [label=\"{EscapeString(task.DisplayName)}\"];");
        }

        sb.AppendLine();

        foreach (var task in workflow.Tasks)
        {
            foreach (var dep in task.DependsOn)
                sb.AppendLine($"    {EscapeId(dep)} -> {EscapeId(task.Id)};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string EscapeId(string id)
    {
        if (id.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return id;
        return $"\"{EscapeString(id)}\"";
    }

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
