namespace WorkflowEngine.Core.Models.ValueObjects;

/// <summary>
/// Value object representing retry configuration for tasks.
/// Encapsulates retry count, delay, and backoff settings.
/// </summary>
public readonly record struct RetrySettings
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Gets the delay between retry attempts in milliseconds.
    /// </summary>
    public int DelayMs { get; init; }

    /// <summary>
    /// Gets whether exponential backoff should be used.
    /// </summary>
    public bool UseExponentialBackoff { get; init; }

    /// <summary>
    /// Gets the maximum delay when using exponential backoff.
    /// </summary>
    public int MaxDelayMs { get; init; }

    /// <summary>
    /// Gets whether retry is enabled.
    /// </summary>
    public bool IsEnabled => MaxRetries > 0;

    /// <summary>
    /// Gets the delay as a TimeSpan.
    /// </summary>
    public TimeSpan Delay => TimeSpan.FromMilliseconds(DelayMs);

    /// <summary>
    /// Gets the maximum delay as a TimeSpan.
    /// </summary>
    public TimeSpan MaxDelay => TimeSpan.FromMilliseconds(MaxDelayMs);

    /// <summary>
    /// Creates retry settings with no retries (disabled).
    /// </summary>
    public static RetrySettings None => new()
    {
        MaxRetries = 0,
        DelayMs = 0,
        UseExponentialBackoff = false,
        MaxDelayMs = 0
    };

    /// <summary>
    /// Creates retry settings with fixed delay.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="delayMs">Delay between retries in milliseconds.</param>
    public static RetrySettings Fixed(int maxRetries, int delayMs = 1000) => new()
    {
        MaxRetries = maxRetries,
        DelayMs = delayMs,
        UseExponentialBackoff = false,
        MaxDelayMs = delayMs
    };

    /// <summary>
    /// Creates retry settings with exponential backoff.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds.</param>
    public static RetrySettings Exponential(int maxRetries, int initialDelayMs = 1000, int maxDelayMs = 30000) => new()
    {
        MaxRetries = maxRetries,
        DelayMs = initialDelayMs,
        UseExponentialBackoff = true,
        MaxDelayMs = maxDelayMs
    };

    /// <summary>
    /// Calculates the delay for a specific retry attempt.
    /// </summary>
    /// <param name="attempt">The retry attempt number (1-based).</param>
    /// <returns>The delay for this attempt.</returns>
    public TimeSpan GetDelayForAttempt(int attempt)
    {
        if (attempt <= 0) return TimeSpan.Zero;

        if (!UseExponentialBackoff)
            return Delay;

        // Exponential backoff: delay * 2^(attempt-1), capped at MaxDelay
        var multiplier = Math.Pow(2, attempt - 1);
        var calculatedDelay = DelayMs * multiplier;
        var cappedDelay = Math.Min(calculatedDelay, MaxDelayMs);
        return TimeSpan.FromMilliseconds(cappedDelay);
    }

    /// <inheritdoc />
    public override string ToString() =>
        !IsEnabled
            ? "no retries"
            : UseExponentialBackoff
                ? $"{MaxRetries} retries (exponential, {DelayMs}ms-{MaxDelayMs}ms)"
                : $"{MaxRetries} retries ({DelayMs}ms delay)";
}
