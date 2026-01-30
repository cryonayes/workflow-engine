namespace WorkflowEngine.Core.Models;

/// <summary>
/// Result of a webhook delivery attempt.
/// </summary>
public sealed class WebhookResult
{
    /// <summary>
    /// Gets whether the webhook was delivered successfully.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the HTTP status code returned by the webhook endpoint.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Gets the error message if the webhook failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the time taken to deliver the webhook.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the number of attempts made to deliver the webhook.
    /// </summary>
    public int Attempts { get; init; } = 1;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="duration">The delivery duration.</param>
    /// <param name="attempts">Number of attempts made.</param>
    /// <returns>A success result.</returns>
    public static WebhookResult Success(int statusCode, TimeSpan duration, int attempts = 1) => new()
    {
        IsSuccess = true,
        StatusCode = statusCode,
        Duration = duration,
        Attempts = attempts
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="duration">The delivery duration.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="attempts">Number of attempts made.</param>
    /// <returns>A failure result.</returns>
    public static WebhookResult Failed(
        string errorMessage,
        TimeSpan duration,
        int? statusCode = null,
        Exception? exception = null,
        int attempts = 1) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        Duration = duration,
        StatusCode = statusCode,
        Exception = exception,
        Attempts = attempts
    };

    /// <inheritdoc />
    public override string ToString() => IsSuccess
        ? $"Webhook succeeded (status: {StatusCode}, duration: {Duration.TotalMilliseconds:F0}ms)"
        : $"Webhook failed: {ErrorMessage} (attempts: {Attempts})";
}
