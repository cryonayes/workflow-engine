using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.FileWatching;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Listeners;

/// <summary>
/// Configuration for the file watch trigger listener.
/// </summary>
public sealed class FileWatchConfig
{
    /// <summary>
    /// Gets or sets the base path to watch.
    /// </summary>
    public required string BasePath { get; init; }

    /// <summary>
    /// Gets or sets the glob patterns for files to include.
    /// </summary>
    public IReadOnlyList<string> Paths { get; init; } = ["**/*"];

    /// <summary>
    /// Gets or sets the glob patterns for files to ignore.
    /// </summary>
    public IReadOnlyList<string> Ignore { get; init; } = [];

    /// <summary>
    /// Gets or sets the debounce duration.
    /// </summary>
    public TimeSpan Debounce { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets whether to watch subdirectories.
    /// </summary>
    public bool IncludeSubdirectories { get; init; } = true;
}

/// <summary>
/// Trigger listener that monitors file system changes.
/// </summary>
public sealed class FileWatchTriggerListener : BaseTriggerListener
{
    private readonly FileWatchConfig _config;
    private readonly GlobMatcher _matcher;
    private FileChangeDebouncer? _debouncer;
    private FileSystemWatcher? _watcher;

    /// <summary>
    /// Initializes a new instance of the FileWatchTriggerListener.
    /// </summary>
    /// <param name="config">The file watch configuration.</param>
    /// <param name="logger">The logger.</param>
    public FileWatchTriggerListener(FileWatchConfig config, ILogger<FileWatchTriggerListener> logger)
        : base(logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        _matcher = new GlobMatcher(
            config.BasePath,
            config.Paths,
            config.Ignore);
    }

    /// <inheritdoc />
    public override TriggerSource Source => TriggerSource.FileWatch;

    /// <inheritdoc />
    protected override Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_config.BasePath))
        {
            throw new DirectoryNotFoundException($"Watch directory not found: {_config.BasePath}");
        }

        // Initialize debouncer
        _debouncer = new FileChangeDebouncer(
            _config.Debounce,
            OnDebouncedChanges);

        // Initialize file system watcher
        _watcher = new FileSystemWatcher(_config.BasePath)
        {
            IncludeSubdirectories = _config.IncludeSubdirectories,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = false // Will enable after setting up handlers
        };

        _watcher.Changed += OnFileSystemEvent;
        _watcher.Created += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Renamed += OnFileSystemRenamed;
        _watcher.Error += OnFileSystemError;

        _watcher.EnableRaisingEvents = true;

        Logger.LogInformation("File watch listener started on {Path}", _config.BasePath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileSystemEvent;
            _watcher.Created -= OnFileSystemEvent;
            _watcher.Deleted -= OnFileSystemEvent;
            _watcher.Renamed -= OnFileSystemRenamed;
            _watcher.Error -= OnFileSystemError;
            _watcher.Dispose();
            _watcher = null;
        }

        _debouncer?.Dispose();
        _debouncer = null;

        Logger.LogInformation("File watch listener stopped");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        // File system watcher uses events, so we just wait here
        return Task.Delay(Timeout.Infinite, cancellationToken);
    }

    /// <inheritdoc />
    public override Task SendResponseAsync(
        IncomingMessage message,
        string response,
        CancellationToken cancellationToken = default)
    {
        // File watch doesn't support responses
        Logger.LogDebug("File watch listener ignoring response: {Response}", response);
        return Task.CompletedTask;
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (!_matcher.IsMatch(e.FullPath))
            {
                Logger.LogTrace("Ignoring change to non-matching file: {Path}", e.FullPath);
                return;
            }

            Logger.LogDebug("File {ChangeType}: {Path}", e.ChangeType, e.FullPath);
            _debouncer?.FileChanged(e.FullPath, e.ChangeType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing file event for {Path}", e.FullPath);
        }
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            // Check if old or new path matches
            var oldMatches = _matcher.IsMatch(e.OldFullPath);
            var newMatches = _matcher.IsMatch(e.FullPath);

            if (oldMatches || newMatches)
            {
                Logger.LogDebug("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
                _debouncer?.FileChanged(e.FullPath, WatcherChangeTypes.Renamed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing rename event for {Path}", e.FullPath);
        }
    }

    private void OnFileSystemError(object sender, ErrorEventArgs e)
    {
        Logger.LogError(e.GetException(), "File system watcher error");

        // Try to recover by restarting the watcher
        try
        {
            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.EnableRaisingEvents = true;
                Logger.LogInformation("File system watcher recovered");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to recover file system watcher");
        }
    }

    private void OnDebouncedChanges(IReadOnlyList<FileChangeInfo> changes)
    {
        Logger.LogInformation("Processing {Count} file change(s)", changes.Count);

        foreach (var change in changes)
        {
            var message = IncomingMessageFactory.FromFileWatch(
                change.FilePath,
                change.ChangeType.ToString(),
                new Dictionary<string, string>
                {
                    ["filePath"] = change.FilePath,
                    ["fileName"] = change.FileName,
                    ["directory"] = change.Directory ?? string.Empty,
                    ["changeType"] = change.ChangeType.ToString(),
                    ["timestamp"] = change.Timestamp.ToString("O")
                });

            PublishMessage(message);
        }
    }
}
