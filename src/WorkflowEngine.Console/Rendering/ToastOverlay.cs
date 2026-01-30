using System.Text.RegularExpressions;
using Spectre.Console;
using WorkflowEngine.Console.Notifications;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Handles overlaying toast notifications onto rendered content.
/// </summary>
internal static partial class ToastOverlay
{
    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex MarkupTagRegex();

    /// <summary>
    /// Overlays active toast notifications onto the rendered lines.
    /// </summary>
    /// <param name="lines">The lines to overlay toasts onto.</param>
    /// <param name="toasts">The toast manager containing active toasts.</param>
    /// <param name="terminalWidth">The terminal width.</param>
    public static void Apply(List<string> lines, ToastManager toasts, int terminalWidth)
    {
        var activeToasts = toasts.GetActive();
        if (activeToasts.Count == 0) return;

        var startRow = 1;

        foreach (var toast in activeToasts.Take(LayoutConstants.MaxToasts))
        {
            var toastLines = toast.Render(LayoutConstants.ToastWidth);
            var xPos = terminalWidth - LayoutConstants.ToastWidth - 2;

            if (xPos < 0) continue;

            for (var i = 0; i < toastLines.Count && startRow + i < lines.Count - 2; i++)
            {
                var lineIndex = startRow + i;
                lines[lineIndex] = OverlayText(lines[lineIndex], toastLines[i], xPos);
            }

            startRow += toastLines.Count + 1;
        }
    }

    /// <summary>
    /// Overlays text onto a base line at the specified position.
    /// </summary>
    private static string OverlayText(string baseLine, string overlay, int position)
    {
        var baseVisible = StripMarkup(baseLine);

        if (baseVisible.Length < position)
            baseVisible = baseVisible.PadRight(position);

        var basePart = baseVisible[..Math.Min(position, baseVisible.Length)];
        return $"[grey]{Markup.Escape(basePart)}[/]{overlay}";
    }

    /// <summary>
    /// Strips Spectre.Console markup tags from text.
    /// </summary>
    public static string StripMarkup(string text)
    {
        return MarkupTagRegex().Replace(text, string.Empty);
    }
}
