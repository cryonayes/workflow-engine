using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Executes individual workflow tasks.
/// </summary>
public interface ITaskExecutor
{
    /// <summary>
    /// Executes a task and returns the result.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="context">The workflow context containing environment and previous results.</param>
    /// <param name="progress">Optional progress reporter for real-time output.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>The task execution result.</returns>
    Task<TaskResult> ExecuteAsync(
        WorkflowTask task,
        WorkflowContext context,
        IProgress<TaskProgress>? progress = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Progress report from task execution.
/// </summary>
/// <param name="TaskId">The ID of the task reporting progress.</param>
/// <param name="Message">The progress message (typically an output line).</param>
/// <param name="StreamType">The source stream (stdout or stderr).</param>
/// <param name="PercentComplete">Optional completion percentage (0-100).</param>
public record TaskProgress(
    string TaskId,
    string Message,
    OutputStreamType StreamType = OutputStreamType.StdOut,
    double? PercentComplete = null
)
{
    /// <inheritdoc />
    public override string ToString() =>
        StreamType == OutputStreamType.StdErr
            ? $"[{TaskId}:stderr] {Message}"
            : $"[{TaskId}] {Message}";
}
