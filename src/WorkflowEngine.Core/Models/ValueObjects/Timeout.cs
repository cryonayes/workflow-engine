namespace WorkflowEngine.Core.Models.ValueObjects;

/// <summary>
/// Value object representing a timeout duration.
/// Provides factory methods for common time units and validates values.
/// </summary>
public readonly record struct Timeout
{
    private readonly int _milliseconds;

    private Timeout(int milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Timeout cannot be negative");
        _milliseconds = milliseconds;
    }

    /// <summary>
    /// Gets the timeout value in milliseconds.
    /// </summary>
    public int Milliseconds => _milliseconds;

    /// <summary>
    /// Gets the timeout value in seconds.
    /// </summary>
    public double Seconds => _milliseconds / 1000.0;

    /// <summary>
    /// Gets whether this timeout represents no timeout (zero duration).
    /// </summary>
    public bool IsInfinite => _milliseconds == 0;

    /// <summary>
    /// Gets the timeout as a TimeSpan.
    /// </summary>
    public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(_milliseconds);

    /// <summary>
    /// Creates a timeout from milliseconds.
    /// </summary>
    public static Timeout FromMilliseconds(int milliseconds) => new(milliseconds);

    /// <summary>
    /// Creates a timeout from seconds.
    /// </summary>
    public static Timeout FromSeconds(int seconds) => new(seconds * 1000);

    /// <summary>
    /// Creates a timeout from minutes.
    /// </summary>
    public static Timeout FromMinutes(int minutes) => new(minutes * 60 * 1000);

    /// <summary>
    /// Creates a timeout from a TimeSpan.
    /// </summary>
    public static Timeout FromTimeSpan(TimeSpan timeSpan) => new((int)timeSpan.TotalMilliseconds);

    /// <summary>
    /// Returns the default timeout (5 minutes).
    /// </summary>
    public static Timeout Default => FromMinutes(5);

    /// <summary>
    /// Returns a zero timeout (no limit).
    /// </summary>
    public static Timeout None => new(0);

    /// <summary>
    /// Implicit conversion from int (milliseconds).
    /// </summary>
    public static implicit operator Timeout(int milliseconds) => FromMilliseconds(milliseconds);

    /// <summary>
    /// Implicit conversion to int (milliseconds).
    /// </summary>
    public static implicit operator int(Timeout timeout) => timeout.Milliseconds;

    /// <inheritdoc />
    public override string ToString() =>
        _milliseconds == 0 ? "infinite" : $"{_milliseconds}ms";
}
