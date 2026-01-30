using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Execution;

/// <summary>
/// Default retry policy with configurable delay and optional exponential backoff.
/// </summary>
public sealed class DefaultRetryPolicy : IRetryPolicy
{
    private readonly ILogger<DefaultRetryPolicy> _logger;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public DefaultRetryPolicy(ILogger<DefaultRetryPolicy> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetrySettings settings,
        Action<int, Exception>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(settings);

        var attempt = 0;
        var lastException = default(Exception);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await operation(cancellationToken);

                if (!ShouldRetry(result) || attempt >= settings.MaxRetries)
                {
                    return result;
                }

                _logger.LogDebug(
                    "Operation returned retryable result on attempt {Attempt}, retrying...",
                    attempt + 1);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt >= settings.MaxRetries)
                {
                    _logger.LogWarning(
                        ex,
                        "Operation failed after {Attempts} attempts, not retrying",
                        attempt + 1);
                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Operation failed on attempt {Attempt} with {ExceptionType}, will retry",
                    attempt + 1,
                    ex.GetType().Name);

                onRetry?.Invoke(attempt + 1, ex);
            }

            attempt++;
            var delay = CalculateDelay(attempt, settings);

            _logger.LogDebug(
                "Waiting {DelayMs}ms before retry attempt {Attempt}",
                delay,
                attempt + 1);

            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <inheritdoc />
    public bool ShouldRetry<T>(T result)
    {
        // For TaskResult, retry if failed
        if (result is TaskResult taskResult)
        {
            return taskResult.IsFailed && !taskResult.WasSkipped;
        }

        return false;
    }

    private static int CalculateDelay(int attempt, RetrySettings settings)
    {
        if (!settings.UseExponentialBackoff)
        {
            return settings.DelayMs;
        }

        return (int)BackoffCalculator.Calculate(attempt, settings.DelayMs, settings.MaxDelayMs).TotalMilliseconds;
    }
}
