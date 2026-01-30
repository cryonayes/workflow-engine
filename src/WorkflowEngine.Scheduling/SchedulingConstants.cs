namespace WorkflowEngine.Scheduling;

/// <summary>
/// Constants for the scheduling system.
/// </summary>
public static class SchedulingConstants
{
    /// <summary>
    /// Interval between scheduler ticks to check for due schedules.
    /// </summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Timeout for waiting for running tasks to complete during shutdown.
    /// </summary>
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default heartbeat interval for background processes.
    /// </summary>
    public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(45);
}
