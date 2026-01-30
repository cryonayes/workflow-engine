namespace WorkflowEngine.Core.Utilities;

/// <summary>
/// Parses duration strings into TimeSpan values.
/// Supports formats: "500ms", "1s", "1.5s", "1m", or raw milliseconds.
/// </summary>
public static class DurationParser
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Parses a duration string into a TimeSpan.
    /// </summary>
    /// <param name="duration">The duration string (e.g., "500ms", "1s", "1m").</param>
    /// <returns>The parsed TimeSpan, or default of 500ms if invalid.</returns>
    public static TimeSpan Parse(string? duration)
    {
        return TryParse(duration, out var result) ? result : DefaultDuration;
    }

    /// <summary>
    /// Attempts to parse a duration string into a TimeSpan.
    /// </summary>
    /// <param name="duration">The duration string to parse.</param>
    /// <param name="result">The parsed TimeSpan if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string? duration, out TimeSpan result)
    {
        result = DefaultDuration;

        if (string.IsNullOrWhiteSpace(duration))
            return false;

        duration = duration.Trim().ToLowerInvariant();

        // Parse durations like "500ms", "1s", "1m"
        if (duration.EndsWith("ms"))
        {
            if (int.TryParse(duration[..^2], out var ms))
            {
                result = TimeSpan.FromMilliseconds(ms);
                return true;
            }
        }
        else if (duration.EndsWith('s'))
        {
            if (double.TryParse(duration[..^1], out var seconds))
            {
                result = TimeSpan.FromSeconds(seconds);
                return true;
            }
        }
        else if (duration.EndsWith('m'))
        {
            if (double.TryParse(duration[..^1], out var minutes))
            {
                result = TimeSpan.FromMinutes(minutes);
                return true;
            }
        }
        else if (int.TryParse(duration, out var rawMs))
        {
            result = TimeSpan.FromMilliseconds(rawMs);
            return true;
        }

        return false;
    }
}
