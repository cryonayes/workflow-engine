namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Provides terminal size information.
/// </summary>
internal static class TerminalInfo
{
    /// <summary>
    /// Gets the current terminal size, with minimum bounds applied.
    /// </summary>
    public static (int Width, int Height) Size =>
        (Math.Max(LayoutConstants.MinWidth, System.Console.WindowWidth),
         Math.Max(10, System.Console.WindowHeight));
}
