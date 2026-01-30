using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Parses workflow definitions from various formats.
/// </summary>
public interface IWorkflowParser
{
    /// <summary>
    /// Parses a workflow from a string content.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>The parsed workflow definition.</returns>
    /// <exception cref="WorkflowParsingException">Thrown if parsing fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown if content is null.</exception>
    Workflow Parse(string content);

    /// <summary>
    /// Parses a workflow from a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The parsed workflow definition.</returns>
    /// <exception cref="WorkflowParsingException">Thrown if parsing fails.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    Workflow ParseFile(string filePath);
}
