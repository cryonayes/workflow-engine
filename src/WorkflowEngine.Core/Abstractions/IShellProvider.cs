namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Provides shell configurations for different shell types.
/// </summary>
public interface IShellProvider
{
    /// <summary>
    /// Gets the shell configuration for the specified shell type.
    /// </summary>
    /// <param name="shellType">The shell type (bash, sh, pwsh, cmd, etc.).</param>
    /// <returns>The shell configuration, or null if not supported.</returns>
    ShellConfiguration? GetShellConfiguration(string shellType);

    /// <summary>
    /// Gets the default shell type for the current platform.
    /// </summary>
    string DefaultShellType { get; }

    /// <summary>
    /// Gets all supported shell types.
    /// </summary>
    IEnumerable<string> SupportedShells { get; }
}

/// <summary>
/// Configuration for a specific shell.
/// </summary>
/// <param name="Executable">The shell executable name or path.</param>
/// <param name="ArgumentTemplate">The argument template with {0} placeholder for the command.</param>
public sealed record ShellConfiguration(string Executable, string[] ArgumentTemplate)
{
    /// <summary>
    /// Builds the command arguments for the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The shell arguments.</returns>
    public string[] BuildArguments(string command) =>
        ArgumentTemplate.Select(t => string.Format(t, command)).ToArray();
}
