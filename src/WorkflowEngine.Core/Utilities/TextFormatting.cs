namespace WorkflowEngine.Core.Utilities;

/// <summary>
/// Shared text formatting utilities.
/// </summary>
public static class TextFormatting
{
    /// <summary>
    /// Truncates text to a maximum length, appending "..." if truncated.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum length (must be at least 4).</param>
    /// <returns>The truncated text.</returns>
    public static string Truncate(string text, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (maxLength < 4) maxLength = 4;

        return text.Length <= maxLength
            ? text
            : text[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Truncates text to a maximum length, appending ellipsis character (…) if truncated.
    /// Suitable for display in limited-width UIs.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>The truncated text.</returns>
    public static string TruncateWithEllipsis(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;
        return text[..(maxLength - 1)] + "…";
    }

    /// <summary>
    /// Truncates text to a maximum length, appending "..." if truncated.
    /// Handles null gracefully by returning empty string.
    /// </summary>
    /// <param name="text">The text to truncate (nullable).</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>The truncated text.</returns>
    public static string TruncateSafe(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;
        return text[..maxLength] + "...";
    }

    /// <summary>
    /// Truncates text for logging purposes, trimming trailing whitespace first.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum length (defaults to GeneralLog limit).</param>
    /// <returns>The truncated text.</returns>
    public static string TruncateForLog(string? text, int maxLength = 100)
    {
        var trimmed = text?.TrimEnd() ?? string.Empty;
        return TruncateSafe(trimmed, maxLength);
    }

    /// <summary>
    /// Formats a duration in a human-readable format.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>Formatted string like "1h 2m 3s" or "45.2s".</returns>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
        if (duration.TotalMinutes >= 1)
            return $"{duration.Minutes}m {duration.Seconds}s";
        return $"{duration.TotalSeconds:F1}s";
    }
}
