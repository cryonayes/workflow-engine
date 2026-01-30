namespace WorkflowEngine.Core.Models;

/// <summary>
/// Represents the execution status of a task or workflow.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>Task is waiting to be executed.</summary>
    Pending,

    /// <summary>Task is currently executing.</summary>
    Running,

    /// <summary>Task completed successfully with exit code 0.</summary>
    Succeeded,

    /// <summary>Task failed due to non-zero exit code or exception.</summary>
    Failed,

    /// <summary>Task was skipped due to condition evaluation.</summary>
    Skipped,

    /// <summary>Task was cancelled by user or system.</summary>
    Cancelled,

    /// <summary>Task exceeded its timeout limit.</summary>
    TimedOut
}

/// <summary>
/// Specifies the type of input data for a task.
/// </summary>
public enum InputType
{
    /// <summary>No input provided.</summary>
    None,

    /// <summary>Plain text input.</summary>
    Text,

    /// <summary>Binary data as base64 encoded string.</summary>
    Bytes,

    /// <summary>Input read from a file path.</summary>
    File,

    /// <summary>Input piped from another task's output.</summary>
    Pipe
}

/// <summary>
/// Specifies how task output should be captured.
/// </summary>
public enum OutputType
{
    /// <summary>Capture output as UTF-8 string.</summary>
    String,

    /// <summary>Capture output as raw bytes.</summary>
    Bytes,

    /// <summary>Write output to a file.</summary>
    File,

    /// <summary>Stream output without buffering (for real-time processing).</summary>
    Stream
}

/// <summary>
/// Identifies the source stream for output events.
/// </summary>
public enum OutputStreamType
{
    /// <summary>Standard output stream.</summary>
    StdOut,

    /// <summary>Standard error stream.</summary>
    StdErr,

    /// <summary>The command being executed (displayed before output).</summary>
    Command
}
