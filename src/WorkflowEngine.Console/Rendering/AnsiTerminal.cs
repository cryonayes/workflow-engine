namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Provides ANSI escape sequence operations for terminal control.
/// </summary>
internal static class AnsiTerminal
{
    /// <summary>
    /// Hides the terminal cursor.
    /// </summary>
    public static void HideCursor() => System.Console.Write("\x1b[?25l");

    /// <summary>
    /// Shows the terminal cursor.
    /// </summary>
    public static void ShowCursor() => System.Console.Write("\x1b[?25h");

    /// <summary>
    /// Clears the screen and moves cursor to home position.
    /// </summary>
    public static void ClearScreen() => System.Console.Write("\x1b[2J\x1b[H");

    /// <summary>
    /// Moves cursor to home position (top-left).
    /// </summary>
    public static void Home() => System.Console.Write("\x1b[H");

    /// <summary>
    /// Clears from cursor position to end of line.
    /// </summary>
    public static void ClearToEndOfLine() => System.Console.Write("\x1b[K");
}
