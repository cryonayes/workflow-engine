using System.Collections.Concurrent;

namespace WorkflowEngine.Triggers.FileWatching;

/// <summary>
/// Debounces rapid file change events, consolidating multiple changes into a single trigger.
/// </summary>
public sealed class FileChangeDebouncer : IDisposable
{
    private readonly TimeSpan _debounceInterval;
    private readonly Action<IReadOnlyList<FileChangeInfo>> _onDebounced;
    private readonly ConcurrentDictionary<string, FileChangeInfo> _pendingChanges = new();
    private readonly object _timerLock = new();
    private Timer? _debounceTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new FileChangeDebouncer.
    /// </summary>
    /// <param name="debounceInterval">The debounce interval.</param>
    /// <param name="onDebounced">Callback invoked when changes are debounced.</param>
    public FileChangeDebouncer(TimeSpan debounceInterval, Action<IReadOnlyList<FileChangeInfo>> onDebounced)
    {
        _debounceInterval = debounceInterval;
        _onDebounced = onDebounced ?? throw new ArgumentNullException(nameof(onDebounced));
    }

    /// <summary>
    /// Records a file change event.
    /// </summary>
    /// <param name="filePath">The path of the changed file.</param>
    /// <param name="changeType">The type of change.</param>
    public void FileChanged(string filePath, WatcherChangeTypes changeType = WatcherChangeTypes.Changed)
    {
        if (_disposed)
            return;

        var changeInfo = new FileChangeInfo(filePath, changeType, DateTimeOffset.UtcNow);

        // Add or update the pending change (last change wins)
        _pendingChanges.AddOrUpdate(filePath, changeInfo, (_, _) => changeInfo);

        // Reset the debounce timer
        ResetTimer();
    }

    /// <summary>
    /// Flushes any pending changes immediately.
    /// </summary>
    public void Flush()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        TriggerDebounced();
    }

    /// <summary>
    /// Disposes the debouncer and any pending timers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        _pendingChanges.Clear();
    }

    private void ResetTimer()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                _ => TriggerDebounced(),
                null,
                _debounceInterval,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void TriggerDebounced()
    {
        if (_disposed)
            return;

        // Collect all pending changes
        var changes = new List<FileChangeInfo>();
        var keys = _pendingChanges.Keys.ToList();

        foreach (var key in keys)
        {
            if (_pendingChanges.TryRemove(key, out var change))
            {
                changes.Add(change);
            }
        }

        if (changes.Count > 0)
        {
            try
            {
                _onDebounced(changes);
            }
            catch
            {
                // Callback errors are ignored to prevent breaking the debouncer
            }
        }
    }
}

/// <summary>
/// Represents information about a file change event.
/// </summary>
/// <param name="FilePath">The full path to the changed file.</param>
/// <param name="ChangeType">The type of change that occurred.</param>
/// <param name="Timestamp">When the change was detected.</param>
public sealed record FileChangeInfo(
    string FilePath,
    WatcherChangeTypes ChangeType,
    DateTimeOffset Timestamp)
{
    /// <summary>
    /// Gets the file name without the directory path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the directory containing the file.
    /// </summary>
    public string? Directory => Path.GetDirectoryName(FilePath);

    /// <summary>
    /// Gets a human-readable description of the change.
    /// </summary>
    public string Description => ChangeType switch
    {
        WatcherChangeTypes.Created => $"Created: {FilePath}",
        WatcherChangeTypes.Deleted => $"Deleted: {FilePath}",
        WatcherChangeTypes.Changed => $"Changed: {FilePath}",
        WatcherChangeTypes.Renamed => $"Renamed: {FilePath}",
        _ => $"{ChangeType}: {FilePath}"
    };
}
