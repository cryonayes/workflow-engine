using System.Text.RegularExpressions;
using Spectre.Console;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Shared helper methods for view renderers.
/// </summary>
internal static partial class RenderHelpers
{
    /// <summary>
    /// Escapes a string for safe Spectre.Console markup rendering.
    /// </summary>
    public static string Escape(string? text) => Markup.Escape(text ?? "");

    /// <summary>
    /// Creates a horizontal rule line of the specified width.
    /// </summary>
    public static string HorizontalRule(int width) => $"[grey]{new string('─', width)}[/]";

    /// <summary>
    /// Pads a list of lines to the specified count by adding empty strings.
    /// </summary>
    public static void PadLines(List<string> lines, int count)
    {
        for (var i = 0; i < count; i++)
            lines.Add("");
    }

    /// <summary>
    /// Builds a visual scroll indicator bar.
    /// </summary>
    public static string BuildScrollIndicator(int percent)
    {
        var width = LayoutConstants.ScrollIndicatorWidth;
        var position = Math.Clamp(percent * width / 100, 0, width);
        return $"│{new string('─', position)}█{new string('─', width - position)}│";
    }

    /// <summary>
    /// Truncates a string to the specified length, adding ellipsis if truncated.
    /// </summary>
    public static string Truncate(string text, int maxLength) =>
        TextFormatting.TruncateWithEllipsis(text, maxLength);

    /// <summary>
    /// Extracts a horizontal slice of a plain text line for scrolling.
    /// </summary>
    public static string SliceLine(string line, int offset, int width)
    {
        if (string.IsNullOrEmpty(line) || offset < 0)
            return line ?? string.Empty;

        if (offset >= line.Length)
            return string.Empty;

        var available = line.Length - offset;
        var take = Math.Min(available, width);
        return line.Substring(offset, take);
    }

    /// <summary>
    /// Slices a markup line for horizontal scrolling.
    /// Strips markup, slices, and returns plain text.
    /// </summary>
    public static string SliceMarkupLine(string line, int offset, int width)
    {
        if (string.IsNullOrEmpty(line) || offset <= 0)
            return line ?? string.Empty;

        var stripped = StripMarkup(line);

        if (offset >= stripped.Length)
            return string.Empty;

        var available = stripped.Length - offset;
        var take = Math.Min(available, width);
        return $"[white]{Escape(stripped.Substring(offset, take))}[/]";
    }

    /// <summary>
    /// Gets the visual length of a markup string (excluding markup tags).
    /// </summary>
    public static int GetVisualLength(string markup) => StripMarkup(markup).Length;

    /// <summary>
    /// Strips Spectre.Console markup tags from a string.
    /// </summary>
    public static string StripMarkup(string markup) =>
        string.IsNullOrEmpty(markup) ? string.Empty : MarkupRegex().Replace(markup, "");

    /// <summary>
    /// Builds the scroll position text (e.g., "1-10/50").
    /// </summary>
    public static string BuildScrollPositionText(int vScroll, int contentHeight, int totalLines)
    {
        var endLine = Math.Min(vScroll + contentHeight, totalLines);
        return $"{vScroll + 1}-{endLine}/{totalLines}";
    }

    /// <summary>
    /// Builds horizontal scroll indicator text.
    /// </summary>
    public static string BuildHorizontalScrollText(int hScroll, int maxLineWidth) =>
        $"← col {hScroll + 1}/{maxLineWidth} →";

    [GeneratedRegex(@"\[/?[^\]]+\]")]
    private static partial Regex MarkupRegex();
}
