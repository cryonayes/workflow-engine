namespace WorkflowEngine.Console.Abstractions;

/// <summary>
/// Provides terminal operations for rendering.
/// </summary>
public interface ITerminalProvider
{
    /// <summary>
    /// Gets the current terminal size.
    /// </summary>
    (int Width, int Height) GetSize();

    /// <summary>
    /// Writes text to the terminal.
    /// </summary>
    /// <param name="text">Text to write.</param>
    void Write(string text);

    /// <summary>
    /// Clears the screen.
    /// </summary>
    void ClearScreen();

    /// <summary>
    /// Moves the cursor to the home position.
    /// </summary>
    void Home();

    /// <summary>
    /// Hides the cursor.
    /// </summary>
    void HideCursor();

    /// <summary>
    /// Shows the cursor.
    /// </summary>
    void ShowCursor();

    /// <summary>
    /// Clears to end of the current line.
    /// </summary>
    void ClearToEndOfLine();
}
