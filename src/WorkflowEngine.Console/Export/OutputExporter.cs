using WorkflowEngine.Console.Notifications;
using WorkflowEngine.Console.State;

namespace WorkflowEngine.Console.Export;

/// <summary>
/// Exports task output to files.
/// </summary>
internal sealed class OutputExporter : IOutputExporter
{
    private readonly ToastManager _toasts;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputExporter"/> class.
    /// </summary>
    /// <param name="toasts">The toast manager for notifications.</param>
    public OutputExporter(ToastManager toasts)
    {
        _toasts = toasts;
    }

    /// <summary>
    /// Exports the output of a task to a file.
    /// </summary>
    /// <param name="task">The task whose output to export.</param>
    public void ExportTaskOutput(TaskInfo task)
    {
        if (task.Output.Count == 0)
        {
            _toasts.Show("No output to export", ToastType.Warning);
            return;
        }

        try
        {
            var sanitizedName = SanitizeFileName(task.Name);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var fileName = $"{sanitizedName}-{timestamp}.log";
            var content = string.Join(Environment.NewLine, task.Output.Select(o => o.Text));

            File.WriteAllText(fileName, content);
            _toasts.Show($"Exported to {fileName}", ToastType.Success);
        }
        catch (Exception ex)
        {
            _toasts.Show($"Export failed: {ex.Message}", ToastType.Error);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name
            .Select(c => char.IsWhiteSpace(c) || invalid.Contains(c) ? '_' : c)
            .ToArray());

        // Collapse consecutive underscores
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");

        return sanitized.Length > 50 ? sanitized[..50].TrimEnd('_') : sanitized;
    }
}
