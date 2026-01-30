namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Builds progress bar strings with Spectre.Console markup.
/// </summary>
internal static class ProgressBar
{
    /// <summary>
    /// Builds a progress bar string with the specified parameters.
    /// </summary>
    /// <param name="percent">Progress percentage (0-100).</param>
    /// <param name="width">Total width of the progress bar in characters.</param>
    /// <param name="filled">Character to use for filled portion.</param>
    /// <param name="empty">Character to use for empty portion.</param>
    /// <param name="filledColor">Spectre.Console color for filled portion.</param>
    /// <param name="emptyColor">Spectre.Console color for empty portion.</param>
    /// <returns>Spectre.Console markup string for the progress bar.</returns>
    public static string Build(int percent, int width, char filled, char empty, string filledColor, string emptyColor)
    {
        var filledCount = percent * width / 100;
        return $"[{filledColor}]{new string(filled, filledCount)}[/]" +
               $"[{emptyColor}]{new string(empty, width - filledCount)}[/]";
    }
}
