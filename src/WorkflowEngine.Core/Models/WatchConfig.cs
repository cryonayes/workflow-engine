namespace WorkflowEngine.Core.Models;

/// <summary>
/// Configuration for file watch mode in workflows.
/// When configured, the workflow will re-run when matching files change.
/// </summary>
public sealed class WatchConfig
{
    /// <summary>
    /// Gets the glob patterns for files to watch.
    /// </summary>
    public IReadOnlyList<string> Paths { get; init; } = [];

    /// <summary>
    /// Gets the glob patterns for files to ignore.
    /// </summary>
    public IReadOnlyList<string> Ignore { get; init; } = [];

    /// <summary>
    /// Gets the debounce duration for file changes.
    /// Multiple changes within this period are consolidated into a single trigger.
    /// </summary>
    public TimeSpan Debounce { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets the specific task IDs to run when files change.
    /// If empty, all tasks are run.
    /// </summary>
    public IReadOnlyList<string> Tasks { get; init; } = [];

    /// <summary>
    /// Gets whether the watch mode is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets whether to run the workflow immediately on start before watching.
    /// </summary>
    public bool RunOnStart { get; init; } = true;

    /// <summary>
    /// Returns true if this configuration is valid for watching.
    /// </summary>
    public bool IsValid => Enabled && Paths.Count > 0;

    /// <inheritdoc />
    public override string ToString() =>
        $"Watch[{Paths.Count} paths, debounce={Debounce.TotalMilliseconds}ms]";
}
