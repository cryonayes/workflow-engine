using WorkflowEngine.Console.Abstractions;

namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Terminal provider using ANSI escape sequences.
/// </summary>
internal sealed class AnsiTerminalProvider : ITerminalProvider
{
    /// <inheritdoc />
    public (int Width, int Height) GetSize() => TerminalInfo.Size;

    /// <inheritdoc />
    public void Write(string text) => System.Console.Write(text);

    /// <inheritdoc />
    public void ClearScreen() => AnsiTerminal.ClearScreen();

    /// <inheritdoc />
    public void Home() => AnsiTerminal.Home();

    /// <inheritdoc />
    public void HideCursor() => AnsiTerminal.HideCursor();

    /// <inheritdoc />
    public void ShowCursor() => AnsiTerminal.ShowCursor();

    /// <inheritdoc />
    public void ClearToEndOfLine() => AnsiTerminal.ClearToEndOfLine();
}
