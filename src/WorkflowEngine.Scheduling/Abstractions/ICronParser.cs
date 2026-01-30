namespace WorkflowEngine.Scheduling.Abstractions;

/// <summary>
/// Parses and evaluates cron expressions.
/// </summary>
public interface ICronParser
{
    /// <summary>
    /// Validates whether a cron expression is syntactically correct.
    /// </summary>
    /// <param name="cronExpression">The cron expression to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValid(string cronExpression);

    /// <summary>
    /// Gets the next occurrence after the specified time.
    /// </summary>
    /// <param name="cronExpression">The cron expression.</param>
    /// <param name="from">The starting time.</param>
    /// <returns>The next occurrence, or null if none within a reasonable range.</returns>
    DateTimeOffset? GetNextOccurrence(string cronExpression, DateTimeOffset from);

    /// <summary>
    /// Gets a human-readable description of the cron expression.
    /// </summary>
    /// <param name="cronExpression">The cron expression.</param>
    /// <returns>A description like "Every day at 2:00 AM".</returns>
    string GetDescription(string cronExpression);
}
