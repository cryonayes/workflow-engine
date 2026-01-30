using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Defines retry behavior for task execution.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="settings">Retry settings from the task.</param>
    /// <param name="onRetry">Optional callback invoked before each retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetrySettings settings,
        Action<int, Exception>? onRetry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a result should trigger a retry.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The result to evaluate.</param>
    /// <returns>True if the operation should be retried.</returns>
    bool ShouldRetry<T>(T result);
}

/// <summary>
/// Settings for retry behavior.
/// </summary>
public sealed record RetrySettings
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Delay between retry attempts in milliseconds.
    /// </summary>
    public int DelayMs { get; init; } = Defaults.RetryDelayMs;

    /// <summary>
    /// Whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; init; }

    /// <summary>
    /// Maximum delay in milliseconds when using exponential backoff.
    /// </summary>
    public int MaxDelayMs { get; init; } = Defaults.MaxRetryDelayMs;

    /// <summary>
    /// Creates retry settings from a workflow task.
    /// </summary>
    public static RetrySettings FromTask(WorkflowTask task) => new()
    {
        MaxRetries = task.RetryCount,
        DelayMs = task.RetryDelayMs
    };

    /// <summary>
    /// Default settings with no retries.
    /// </summary>
    public static RetrySettings None => new() { MaxRetries = 0 };
}
