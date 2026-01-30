using System.Text.RegularExpressions;

namespace WorkflowEngine.Triggers.Storage;

/// <summary>
/// Expands environment variables in configuration strings.
/// </summary>
internal static partial class EnvironmentVariableExpander
{
    /// <summary>
    /// Expands ${VAR_NAME} patterns with environment variable values.
    /// </summary>
    /// <param name="content">The content to expand.</param>
    /// <returns>Content with environment variables expanded.</returns>
    public static string Expand(string content)
    {
        return EnvVarRegex().Replace(content, match =>
        {
            var varName = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(varName) ?? match.Value;
        });
    }

    [GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex EnvVarRegex();
}
