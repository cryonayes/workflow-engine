namespace WorkflowEngine.Console.Rendering;

/// <summary>
/// Constants for UI layout and rendering.
/// </summary>
internal static class LayoutConstants
{
    /// <summary>
    /// Refresh interval in milliseconds.
    /// </summary>
    public const int RefreshMs = 100;

    /// <summary>
    /// Minimum terminal width.
    /// </summary>
    public const int MinWidth = 40;

    /// <summary>
    /// Maximum number of toasts to display.
    /// </summary>
    public const int MaxToasts = 5;

    /// <summary>
    /// Width of toast notifications.
    /// </summary>
    public const int ToastWidth = 45;

    /// <summary>
    /// Width of the scroll indicator.
    /// </summary>
    public const int ScrollIndicatorWidth = 8;

    /// <summary>
    /// Default toast display duration in seconds.
    /// </summary>
    public const double DefaultToastDuration = 3.0;

    /// <summary>
    /// Error toast display duration in seconds.
    /// </summary>
    public const double ErrorToastDuration = 5.0;
}
