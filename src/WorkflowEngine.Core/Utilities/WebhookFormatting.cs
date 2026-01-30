using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Utilities;

/// <summary>
/// Shared webhook formatting utilities.
/// </summary>
public static class WebhookFormatting
{
    /// <summary>
    /// Gets a human-readable title for a webhook event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>A formatted title string.</returns>
    public static string GetEventTitle(WebhookEventType eventType) => eventType switch
    {
        WebhookEventType.WorkflowStarted => "Workflow Started",
        WebhookEventType.WorkflowCompleted => "Workflow Completed",
        WebhookEventType.WorkflowFailed => "Workflow Failed",
        WebhookEventType.WorkflowCancelled => "Workflow Cancelled",
        WebhookEventType.TaskStarted => "Task Started",
        WebhookEventType.TaskCompleted => "Task Completed",
        WebhookEventType.TaskFailed => "Task Failed",
        WebhookEventType.TaskSkipped => "Task Skipped",
        WebhookEventType.TaskTimedOut => "Task Timed Out",
        _ => eventType.ToString()
    };

    /// <summary>
    /// Gets the emoji for an event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>An emoji string.</returns>
    public static string GetEventEmoji(WebhookEventType eventType) => eventType switch
    {
        WebhookEventType.WorkflowStarted => "🚀",
        WebhookEventType.WorkflowCompleted => "✅",
        WebhookEventType.WorkflowFailed => "❌",
        WebhookEventType.WorkflowCancelled => "⚠️",
        WebhookEventType.TaskStarted => "▶️",
        WebhookEventType.TaskCompleted => "✓",
        WebhookEventType.TaskFailed => "✗",
        WebhookEventType.TaskSkipped => "⏭️",
        WebhookEventType.TaskTimedOut => "⏱️",
        _ => "📌"
    };

    /// <summary>
    /// Gets the color code for an event type (used by providers that support colors).
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>A hex color code.</returns>
    public static int GetEventColor(WebhookEventType eventType) => eventType switch
    {
        WebhookEventType.WorkflowStarted or WebhookEventType.TaskStarted => 0x3498DB, // Blue
        WebhookEventType.WorkflowCompleted or WebhookEventType.TaskCompleted => 0x2ECC71, // Green
        WebhookEventType.WorkflowFailed or WebhookEventType.TaskFailed => 0xE74C3C, // Red
        WebhookEventType.WorkflowCancelled => 0xF39C12, // Orange
        WebhookEventType.TaskSkipped => 0x95A5A6, // Gray
        WebhookEventType.TaskTimedOut => 0xE67E22, // Dark Orange
        _ => 0x95A5A6 // Default gray
    };

    /// <summary>
    /// Truncates error text for display, wrapping in code blocks if specified.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="maxLength">Maximum length before truncation.</param>
    /// <param name="wrapInCodeBlock">Whether to wrap in markdown code block.</param>
    /// <returns>The formatted error text.</returns>
    public static string FormatError(string error, int maxLength, bool wrapInCodeBlock = false)
    {
        ArgumentNullException.ThrowIfNull(error);

        var truncated = error.Length <= maxLength
            ? error
            : error[..(maxLength - 3)] + "...";

        return wrapInCodeBlock ? $"```\n{truncated}\n```" : truncated;
    }
}
