namespace WorkflowEngine.Console.State;

/// <summary>
/// Represents the status of a wave during execution.
/// </summary>
internal enum WaveStatus
{
    /// <summary>
    /// Wave is waiting to execute.
    /// </summary>
    Pending,

    /// <summary>
    /// Wave is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Wave has completed execution.
    /// </summary>
    Completed
}
