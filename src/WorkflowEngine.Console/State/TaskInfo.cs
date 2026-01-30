using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.State;

/// <summary>
/// Represents a single line of task output with its stream type.
/// </summary>
internal sealed record OutputLine(string Text, OutputStreamType StreamType);

/// <summary>
/// Information about a task for rendering purposes.
/// </summary>
internal sealed class TaskInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskInfo"/> class.
    /// </summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="name">The task display name.</param>
    /// <param name="waveIndex">The wave index this task belongs to.</param>
    public TaskInfo(string id, string name, int waveIndex)
    {
        Id = id;
        Name = name;
        WaveIndex = waveIndex;
    }

    /// <summary>
    /// Gets the task identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the task display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the wave index this task belongs to.
    /// </summary>
    public int WaveIndex { get; }

    /// <summary>
    /// Gets or sets the current execution status.
    /// </summary>
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    /// <summary>
    /// Gets or sets when the task started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the task duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets the exit code.
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Gets the captured output lines.
    /// </summary>
    public List<OutputLine> Output { get; } = [];
}
