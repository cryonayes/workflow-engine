using Spectre.Console;
using WorkflowEngine.Console.Rendering;

namespace WorkflowEngine.Console.Notifications;

/// <summary>
/// Represents a toast notification.
/// </summary>
internal sealed class Toast
{
    /// <summary>
    /// Gets the toast message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the toast type.
    /// </summary>
    public required ToastType Type { get; init; }

    /// <summary>
    /// Gets when the toast expires.
    /// </summary>
    public required DateTimeOffset Expiry { get; init; }

    /// <summary>
    /// Gets the visual style for the toast type.
    /// </summary>
    public (string Icon, string BorderColor, string BgColor) GetStyle() => Type switch
    {
        ToastType.Success => ("✓", "green", "on grey15"),
        ToastType.Error => ("✗", "red", "on grey15"),
        ToastType.Warning => ("⚠", "yellow", "on grey15"),
        ToastType.Info => ("ℹ", "blue", "on grey15"),
        _ => ("•", "grey", "on grey15")
    };

    /// <summary>
    /// Renders the toast as a list of markup lines.
    /// </summary>
    /// <param name="width">Width of the toast box.</param>
    /// <returns>List of markup lines.</returns>
    public List<string> Render(int width)
    {
        var (icon, borderColor, bgColor) = GetStyle();
        var maxMessageLen = width - 7; // borders (2) + padding (2) + icon + space (3)
        var message = RenderHelpers.Truncate(Message, maxMessageLen);
        var content = $" {icon} {message}".PadRight(width - 2);

        return
        [
            $"[{borderColor}]╭{new string('─', width - 2)}╮[/]",
            $"[{borderColor}]│[/][{bgColor}]{Markup.Escape(content)}[/][{borderColor}]│[/]",
            $"[{borderColor}]╰{new string('─', width - 2)}╯[/]"
        ];
    }
}
