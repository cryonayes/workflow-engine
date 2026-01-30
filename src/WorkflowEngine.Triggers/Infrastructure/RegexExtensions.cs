using System.Text.RegularExpressions;

namespace WorkflowEngine.Triggers.Infrastructure;

/// <summary>
/// Extension methods for regex operations.
/// </summary>
internal static class RegexExtensions
{
    /// <summary>
    /// Extracts named capture groups from a regex match into a dictionary.
    /// </summary>
    /// <param name="match">The regex match.</param>
    /// <param name="regex">The regex used for the match.</param>
    /// <returns>Dictionary of capture group names to values.</returns>
    public static IReadOnlyDictionary<string, string> ExtractNamedCaptures(this Match match, Regex regex)
    {
        var captures = new Dictionary<string, string>();

        foreach (var groupName in regex.GetGroupNames())
        {
            // Skip the implicit "0" group (full match)
            if (groupName == "0")
                continue;

            var group = match.Groups[groupName];
            if (group.Success)
            {
                captures[groupName] = group.Value;
            }
        }

        return captures;
    }
}
