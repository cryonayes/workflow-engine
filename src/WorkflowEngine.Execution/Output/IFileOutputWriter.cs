using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Output;

/// <summary>
/// Writes task output to files with security validation.
/// </summary>
public interface IFileOutputWriter
{
    /// <summary>
    /// Writes output to a file.
    /// </summary>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="stdOut">The standard output content.</param>
    /// <param name="stdErr">The standard error content.</param>
    /// <param name="config">The output configuration.</param>
    /// <returns>A TaskOutput with file path or fallback content.</returns>
    /// <exception cref="InvalidOperationException">Thrown when path traversal is detected.</exception>
    TaskOutput WriteToFile(string filePath, string stdOut, string stdErr, TaskOutputConfig config);
}
