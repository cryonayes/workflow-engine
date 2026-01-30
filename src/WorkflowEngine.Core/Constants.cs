namespace WorkflowEngine.Core;

/// <summary>
/// Global constants for the workflow engine.
/// </summary>
public static class Defaults
{
    /// <summary>Default task timeout: 5 minutes.</summary>
    public const int TimeoutMs = 300_000;

    /// <summary>Default retry delay: 1 second.</summary>
    public const int RetryDelayMs = 1_000;

    /// <summary>Maximum retry delay for exponential backoff: 30 seconds.</summary>
    public const int MaxRetryDelayMs = 30_000;

    /// <summary>Maximum output size: 10 MB.</summary>
    public const long MaxOutputSizeBytes = 10 * 1024 * 1024;

    /// <summary>Default shell on Unix systems.</summary>
    public const string UnixShell = "bash";

    /// <summary>Default shell on Windows systems.</summary>
    public const string WindowsShell = "cmd";
}

/// <summary>
/// Logging and display truncation limits.
/// </summary>
public static class TruncationLimits
{
    /// <summary>Maximum length for command logging.</summary>
    public const int CommandLog = 200;

    /// <summary>Maximum length for error messages in results.</summary>
    public const int ErrorMessage = 500;

    /// <summary>Maximum length for general log output.</summary>
    public const int GeneralLog = 100;
}

/// <summary>
/// Numeric precision constants.
/// </summary>
public static class Precision
{
    /// <summary>Epsilon for floating-point comparisons.</summary>
    public const double FloatEpsilon = 0.0001;
}

/// <summary>
/// String manipulation helpers.
/// Delegates to <see cref="Utilities.TextFormatting"/> for actual implementations.
/// </summary>
public static class StringHelpers
{
    /// <summary>
    /// Truncates a string to the specified length, appending "..." if truncated.
    /// </summary>
    public static string Truncate(string value, int maxLength) =>
        Utilities.TextFormatting.TruncateSafe(value, maxLength);

    /// <summary>
    /// Truncates a string for logging purposes.
    /// </summary>
    public static string TruncateForLog(string value, int maxLength = TruncationLimits.GeneralLog) =>
        Utilities.TextFormatting.TruncateForLog(value, maxLength);
}
