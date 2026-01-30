namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Provides text formatting utilities.
/// </summary>
internal static class TextFormatter
{
    /// <summary>
    /// Formats a duration as a human-readable string.
    /// </summary>
    public static string FormatDuration(TimeSpan d) =>
        d.TotalMinutes >= 1 ? $"{(int)d.TotalMinutes}m{d.Seconds}s" : $"{d.TotalSeconds:F1}s";
}
