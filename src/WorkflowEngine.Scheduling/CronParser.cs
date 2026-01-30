using Cronos;
using WorkflowEngine.Scheduling.Abstractions;

namespace WorkflowEngine.Scheduling;

/// <summary>
/// Parses and evaluates cron expressions using the Cronos library.
/// </summary>
public sealed class CronParser : ICronParser
{
    private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.Local;

    /// <inheritdoc />
    public bool IsValid(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        return TryParseExpression(cronExpression, out _);
    }

    /// <inheritdoc />
    public DateTimeOffset? GetNextOccurrence(string cronExpression, DateTimeOffset from)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        var cron = ParseExpression(cronExpression);
        var next = cron.GetNextOccurrence(from.UtcDateTime, TimeZoneInfo.Utc);

        return next.HasValue ? new DateTimeOffset(next.Value, TimeSpan.Zero) : null;
    }

    /// <inheritdoc />
    public string GetDescription(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return "Invalid expression";

        try
        {
            var cron = ParseExpression(cronExpression);
            return DescribeCronExpression(cronExpression, cron);
        }
        catch
        {
            return "Invalid cron expression";
        }
    }

    private static bool TryParseExpression(string cronExpression, out CronExpression? result)
    {
        // Try parsing with seconds first, then standard format
        try
        {
            result = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            return true;
        }
        catch
        {
            try
            {
                result = CronExpression.Parse(cronExpression, CronFormat.Standard);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }

    private static CronExpression ParseExpression(string cronExpression)
    {
        if (!TryParseExpression(cronExpression, out var result) || result is null)
        {
            throw new ArgumentException($"Invalid cron expression: {cronExpression}", nameof(cronExpression));
        }
        return result;
    }

    private static string DescribeCronExpression(string expr, CronExpression cron)
    {
        // Provide human-readable descriptions for common patterns
        var parts = expr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Standard 5-field format: minute hour day month weekday
        if (parts.Length == 5)
        {
            return (parts[0], parts[1], parts[2], parts[3], parts[4]) switch
            {
                ("*", "*", "*", "*", "*") => "Every minute",
                ("0", "*", "*", "*", "*") => "Every hour",
                ("0", "0", "*", "*", "*") => "Every day at midnight",
                ("0", var h, "*", "*", "*") when int.TryParse(h, out var hour) =>
                    $"Every day at {FormatHour(hour)}",
                (var m, var h, "*", "*", "*") when int.TryParse(m, out var min) && int.TryParse(h, out var hour) =>
                    $"Every day at {FormatTime(hour, min)}",
                ("0", "0", "*", "*", "0") => "Every Sunday at midnight",
                ("0", "0", "*", "*", "1") => "Every Monday at midnight",
                ("0", "0", "1", "*", "*") => "First day of every month at midnight",
                _ => $"Cron: {expr}"
            };
        }

        // 6-field format with seconds
        if (parts.Length == 6)
        {
            return (parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]) switch
            {
                ("*", "*", "*", "*", "*", "*") => "Every second",
                ("0", "*", "*", "*", "*", "*") => "Every minute",
                ("0", "0", "*", "*", "*", "*") => "Every hour",
                _ => $"Cron: {expr}"
            };
        }

        return $"Cron: {expr}";
    }

    private static string FormatHour(int hour) =>
        hour switch
        {
            0 => "midnight",
            12 => "noon",
            < 12 => $"{hour}:00 AM",
            _ => $"{hour - 12}:00 PM"
        };

    private static string FormatTime(int hour, int minute)
    {
        var period = hour < 12 ? "AM" : "PM";
        var displayHour = hour switch
        {
            0 => 12,
            > 12 => hour - 12,
            _ => hour
        };
        return $"{displayHour}:{minute:D2} {period}";
    }
}
