namespace WorkflowEngine.Runner;

/// <summary>
/// Thread-safe execution statistics. All fields must be accessed via Interlocked operations.
/// </summary>
public sealed class ExecutionStats
{
    private int _succeeded;
    private int _failed;
    private int _skipped;
    private int _totalCompleted;
    private int _taskIndex;

    /// <summary>
    /// Gets the number of succeeded tasks.
    /// </summary>
    public int Succeeded => Volatile.Read(ref _succeeded);

    /// <summary>
    /// Gets the number of failed tasks.
    /// </summary>
    public int Failed => Volatile.Read(ref _failed);

    /// <summary>
    /// Gets the number of skipped tasks.
    /// </summary>
    public int Skipped => Volatile.Read(ref _skipped);

    /// <summary>
    /// Gets the current task index.
    /// </summary>
    public int TaskIndex => Volatile.Read(ref _taskIndex);

    /// <summary>
    /// Gets the total number of completed tasks (atomic read).
    /// </summary>
    public int TotalCompleted => Volatile.Read(ref _totalCompleted);

    /// <summary>
    /// Gets an atomic snapshot of all statistics.
    /// Use this when you need consistent values across multiple fields.
    /// </summary>
    /// <returns>A snapshot containing all current statistics values.</returns>
    public StatsSnapshot GetSnapshot()
    {
        // Read total first as it's the aggregate
        var total = Volatile.Read(ref _totalCompleted);
        return new StatsSnapshot(
            Succeeded: Volatile.Read(ref _succeeded),
            Failed: Volatile.Read(ref _failed),
            Skipped: Volatile.Read(ref _skipped),
            TotalCompleted: total,
            TaskIndex: Volatile.Read(ref _taskIndex));
    }

    /// <summary>
    /// Increments the succeeded count.
    /// </summary>
    public int IncrementSucceeded()
    {
        Interlocked.Increment(ref _totalCompleted);
        return Interlocked.Increment(ref _succeeded);
    }

    /// <summary>
    /// Increments the failed count.
    /// </summary>
    public int IncrementFailed()
    {
        Interlocked.Increment(ref _totalCompleted);
        return Interlocked.Increment(ref _failed);
    }

    /// <summary>
    /// Increments the skipped count.
    /// </summary>
    public int IncrementSkipped()
    {
        Interlocked.Increment(ref _totalCompleted);
        return Interlocked.Increment(ref _skipped);
    }

    /// <summary>
    /// Increments the task index.
    /// </summary>
    public int IncrementTaskIndex() => Interlocked.Increment(ref _taskIndex);
}

/// <summary>
/// Immutable snapshot of execution statistics for thread-safe reading.
/// </summary>
/// <param name="Succeeded">Number of succeeded tasks.</param>
/// <param name="Failed">Number of failed tasks.</param>
/// <param name="Skipped">Number of skipped tasks.</param>
/// <param name="TotalCompleted">Total completed tasks.</param>
/// <param name="TaskIndex">Current task index.</param>
public readonly record struct StatsSnapshot(
    int Succeeded,
    int Failed,
    int Skipped,
    int TotalCompleted,
    int TaskIndex);
