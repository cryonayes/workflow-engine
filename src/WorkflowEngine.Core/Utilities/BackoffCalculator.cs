namespace WorkflowEngine.Core.Utilities;

/// <summary>
/// Provides exponential backoff delay calculations with optional jitter.
/// </summary>
public static class BackoffCalculator
{
    private const int DefaultMaxDelaySeconds = 60;
    private const double DefaultJitterFactor = 0.3;

    /// <summary>
    /// Calculates exponential backoff delay.
    /// </summary>
    /// <param name="attempt">The retry attempt number (1-based).</param>
    /// <param name="baseDelayMs">Base delay in milliseconds.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds.</param>
    /// <param name="useJitter">Whether to add random jitter to the delay.</param>
    /// <returns>The calculated delay.</returns>
    public static TimeSpan Calculate(
        int attempt,
        int baseDelayMs = 1000,
        int maxDelayMs = DefaultMaxDelaySeconds * 1000,
        bool useJitter = false)
    {
        var delay = baseDelayMs * Math.Pow(2, attempt - 1);
        delay = Math.Min(delay, maxDelayMs);

        if (useJitter)
        {
            var jitter = Random.Shared.NextDouble() * DefaultJitterFactor * delay;
            delay += jitter;
        }

        return TimeSpan.FromMilliseconds(delay);
    }

    /// <summary>
    /// Calculates exponential backoff delay in seconds with jitter.
    /// </summary>
    /// <param name="attempt">The retry attempt number (1-based).</param>
    /// <param name="maxDelaySeconds">Maximum delay in seconds.</param>
    /// <returns>The calculated delay with jitter.</returns>
    public static TimeSpan CalculateWithJitter(int attempt, int maxDelaySeconds = DefaultMaxDelaySeconds)
    {
        var baseDelay = Math.Min(Math.Pow(2, attempt), maxDelaySeconds);
        var jitter = Random.Shared.NextDouble() * DefaultJitterFactor * baseDelay;
        return TimeSpan.FromSeconds(baseDelay + jitter);
    }
}
