namespace WorkflowEngine.Triggers;

/// <summary>
/// Constants for the trigger system.
/// </summary>
public static class TriggerConstants
{
    /// <summary>
    /// Default capacity for the message queue channel.
    /// </summary>
    public const int DefaultMessageQueueCapacity = 1000;

    /// <summary>
    /// Timeout for waiting for message processing to complete during shutdown.
    /// </summary>
    public static readonly TimeSpan ProcessingShutdownTimeout = TimeSpan.FromSeconds(5);
}
