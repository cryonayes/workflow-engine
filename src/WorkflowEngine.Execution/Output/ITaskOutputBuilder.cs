using System.Text;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Output;

/// <summary>
/// Builds task output from stdout/stderr streams with size limits and format handling.
/// </summary>
public interface ITaskOutputBuilder
{
    /// <summary>
    /// Builds a TaskOutput from stdout and stderr content.
    /// </summary>
    /// <param name="task">The task being executed.</param>
    /// <param name="stdOut">The standard output content.</param>
    /// <param name="stdErr">The standard error content.</param>
    /// <returns>A configured TaskOutput instance.</returns>
    TaskOutput Build(WorkflowTask task, StringBuilder stdOut, StringBuilder stdErr);
}
