using WorkflowEngine.Core.Extensions;

namespace WorkflowEngine.Core.Models;

/// <summary>
/// Represents the result of a task execution including output, status, and timing.
/// </summary>
public sealed class TaskResult
{
    /// <summary>
    /// Gets the ID of the task that produced this result.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// Gets the execution status of the task.
    /// </summary>
    public ExecutionStatus Status { get; init; }

    /// <summary>
    /// Process exit code. 0 = success, -1 = failed before execution.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the captured output from the task.
    /// </summary>
    public TaskOutput? Output { get; init; }

    /// <summary>
    /// Gets the error message if the task failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets when the task started executing.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets when the task finished executing.
    /// </summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Gets the total duration of task execution.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Gets whether the task completed successfully (status is Succeeded and exit code is 0).
    /// </summary>
    public bool IsSuccess => Status.IsSuccessful(ExitCode);

    /// <summary>
    /// Gets whether the task was skipped due to condition evaluation.
    /// </summary>
    public bool WasSkipped => Status == ExecutionStatus.Skipped;

    /// <summary>
    /// Gets whether the task was cancelled.
    /// </summary>
    public bool IsCancelled => Status == ExecutionStatus.Cancelled;

    /// <summary>
    /// Gets whether the task failed (any failure status including cancellation).
    /// </summary>
    public bool IsFailed => Status is ExecutionStatus.Failed or ExecutionStatus.TimedOut or ExecutionStatus.Cancelled;

    /// <summary>
    /// Creates a result for a skipped task.
    /// </summary>
    public static TaskResult Skipped(string taskId, string reason) => new()
    {
        TaskId = taskId,
        Status = ExecutionStatus.Skipped,
        ExitCode = 0,
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow,
        ErrorMessage = reason
    };

    /// <summary>
    /// Creates a result for a cancelled task.
    /// </summary>
    public static TaskResult Cancelled(string taskId, DateTimeOffset startTime) => new()
    {
        TaskId = taskId,
        Status = ExecutionStatus.Cancelled,
        ExitCode = -1,
        StartTime = startTime,
        EndTime = DateTimeOffset.UtcNow,
        ErrorMessage = "Task was cancelled"
    };

    /// <summary>
    /// Creates a result for a failed task.
    /// </summary>
    public static TaskResult Failed(string taskId, DateTimeOffset startTime, string error, Exception? ex = null) => new()
    {
        TaskId = taskId,
        Status = ExecutionStatus.Failed,
        ExitCode = -1,
        StartTime = startTime,
        EndTime = DateTimeOffset.UtcNow,
        ErrorMessage = error,
        Exception = ex
    };

    /// <summary>
    /// Creates a result for a task that timed out.
    /// </summary>
    public static TaskResult TimedOut(string taskId, DateTimeOffset startTime, TimeSpan timeout, TaskOutput? output = null) => new()
    {
        TaskId = taskId,
        Status = ExecutionStatus.TimedOut,
        ExitCode = -1,
        StartTime = startTime,
        EndTime = DateTimeOffset.UtcNow,
        ErrorMessage = $"Task exceeded timeout of {timeout.TotalSeconds:F1}s",
        Output = output
    };

    /// <inheritdoc />
    public override string ToString() =>
        $"TaskResult[{TaskId}] {Status} (exit: {ExitCode}, duration: {Duration.TotalSeconds:F2}s)";
}
