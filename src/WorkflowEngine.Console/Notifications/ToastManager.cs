using WorkflowEngine.Console.Rendering;

namespace WorkflowEngine.Console.Notifications;

/// <summary>
/// Manages toast notifications with automatic expiry.
/// </summary>
internal sealed class ToastManager
{
    private readonly List<Toast> _toasts = [];

    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="type">The type of toast.</param>
    /// <param name="durationSeconds">Optional custom duration in seconds.</param>
    public void Show(string message, ToastType type, double? durationSeconds = null)
    {
        var duration = durationSeconds ?? (type == ToastType.Error
            ? LayoutConstants.ErrorToastDuration
            : LayoutConstants.DefaultToastDuration);

        _toasts.Add(new Toast
        {
            Message = message,
            Type = type,
            Expiry = DateTimeOffset.UtcNow.AddSeconds(duration)
        });
    }

    /// <summary>
    /// Gets the list of active (non-expired) toasts.
    /// </summary>
    /// <returns>List of active toasts.</returns>
    public List<Toast> GetActive()
    {
        var now = DateTimeOffset.UtcNow;
        _toasts.RemoveAll(t => t.Expiry <= now);
        return _toasts.ToList();
    }
}
