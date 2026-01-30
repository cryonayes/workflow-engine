using WorkflowEngine.Console.State;

namespace WorkflowEngine.Console.Export;

/// <summary>
/// Exports task output to files.
/// </summary>
internal interface IOutputExporter
{
    /// <summary>
    /// Exports the output of a task to a file.
    /// </summary>
    /// <param name="task">The task whose output to export.</param>
    void ExportTaskOutput(TaskInfo task);
}
