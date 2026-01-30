namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Interface for processing Slack events with signature verification.
/// Used by HTTP listener to delegate Slack event handling.
/// </summary>
public interface ISlackEventProcessor
{
    /// <summary>
    /// Processes a Slack event with signature verification.
    /// </summary>
    /// <param name="body">The raw request body.</param>
    /// <param name="timestamp">The X-Slack-Request-Timestamp header value.</param>
    /// <param name="signature">The X-Slack-Signature header value.</param>
    /// <returns>The response body if valid, null if signature verification failed.</returns>
    string? ProcessSlackEvent(string body, string timestamp, string signature);
}
