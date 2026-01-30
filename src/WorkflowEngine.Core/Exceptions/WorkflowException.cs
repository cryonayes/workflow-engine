namespace WorkflowEngine.Core.Exceptions;

/// <summary>
/// Base exception for all workflow engine errors.
/// </summary>
public class WorkflowException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowException"/> class.
    /// </summary>
    public WorkflowException() { }

    /// <summary>
    /// Initializes a new instance with a message.
    /// </summary>
    public WorkflowException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    public WorkflowException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when workflow parsing or validation fails.
/// </summary>
public class WorkflowParsingException : WorkflowException
{
    /// <summary>
    /// Gets the validation errors that caused the exception.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance with validation errors.
    /// </summary>
    public WorkflowParsingException(string message, IReadOnlyList<ValidationError> errors)
        : base(FormatMessage(message, errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance with a single error message.
    /// </summary>
    public WorkflowParsingException(string message)
        : base(message)
    {
        Errors = new[] { new ValidationError(message) };
    }

    private static string FormatMessage(string message, IReadOnlyList<ValidationError> errors)
    {
        if (errors.Count == 0)
            return message;

        return $"{message}: {string.Join("; ", errors.Select(e => e.Message))}";
    }
}

/// <summary>
/// Exception thrown when a circular dependency is detected in the workflow.
/// </summary>
public class CircularDependencyException : WorkflowException
{
    /// <summary>
    /// Gets the description of the circular dependency path.
    /// </summary>
    public string CyclePath { get; }

    /// <summary>
    /// Initializes a new instance with the cycle path.
    /// </summary>
    public CircularDependencyException(string cyclePath)
        : base($"Circular dependency detected: {cyclePath}")
    {
        CyclePath = cyclePath;
    }
}

/// <summary>
/// Exception thrown when task execution fails.
/// </summary>
public class TaskExecutionException : WorkflowException
{
    /// <summary>
    /// Gets the ID of the failed task.
    /// </summary>
    public string TaskId { get; }

    /// <summary>
    /// Gets the exit code of the failed process, if available.
    /// </summary>
    public int? ExitCode { get; }

    /// <summary>
    /// Initializes a new instance for a task failure.
    /// </summary>
    public TaskExecutionException(string taskId, string message, int? exitCode = null, Exception? innerException = null)
        : base($"Task '{taskId}' failed: {message}", innerException!)
    {
        TaskId = taskId;
        ExitCode = exitCode;
    }
}

/// <summary>
/// Exception thrown when a task times out.
/// </summary>
public class TaskTimeoutException : TaskExecutionException
{
    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance for a task timeout.
    /// </summary>
    public TaskTimeoutException(string taskId, TimeSpan timeout)
        : base(taskId, $"Task exceeded timeout of {timeout.TotalSeconds:F1}s")
    {
        Timeout = timeout;
    }
}

/// <summary>
/// Exception thrown when an expression evaluation fails.
/// </summary>
public class ExpressionEvaluationException : WorkflowException
{
    /// <summary>
    /// Gets the expression that failed to evaluate.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Initializes a new instance for an expression evaluation failure.
    /// </summary>
    public ExpressionEvaluationException(string expression, string message, Exception? innerException = null)
        : base($"Failed to evaluate expression '{expression}': {message}", innerException!)
    {
        Expression = expression;
    }
}

/// <summary>
/// Represents a validation error during workflow parsing.
/// </summary>
/// <param name="Code">Error code for programmatic handling.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="TaskId">Optional task ID where the error occurred.</param>
/// <param name="Line">The line number where the error occurred, if known.</param>
/// <param name="Column">The column number where the error occurred, if known.</param>
public sealed record ValidationError(
    string Code,
    string Message,
    string? TaskId = null,
    int? Line = null,
    int? Column = null)
{
    /// <summary>
    /// Creates a validation error with just a message (for backward compatibility).
    /// </summary>
    public ValidationError(string message)
        : this("ERR", message, null, null, null) { }

    /// <inheritdoc />
    public override string ToString() =>
        Line.HasValue ? $"[{Code}] Line {Line}: {Message}" : $"[{Code}] {Message}";
}
