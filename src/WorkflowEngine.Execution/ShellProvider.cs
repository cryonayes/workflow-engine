using WorkflowEngine.Core.Abstractions;

namespace WorkflowEngine.Execution;

/// <summary>
/// Default shell provider with common shell configurations.
/// </summary>
public sealed class DefaultShellProvider : IShellProvider
{
    private readonly Dictionary<string, ShellConfiguration> _shells;

    /// <summary>
    /// Initializes a new instance with default shell configurations.
    /// </summary>
    public DefaultShellProvider()
    {
        _shells = new Dictionary<string, ShellConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["bash"] = new("bash", ["-c", "{0}"]),
            ["sh"] = new("sh", ["-c", "{0}"]),
            ["zsh"] = new("zsh", ["-c", "{0}"]),
            ["pwsh"] = new("pwsh", ["-Command", "{0}"]),
            ["powershell"] = new("pwsh", ["-Command", "{0}"]),
            ["cmd"] = new("cmd", ["/c", "{0}"])
        };
    }

    /// <summary>
    /// Initializes a new instance with custom shell configurations.
    /// </summary>
    /// <param name="customShells">Additional shell configurations to register.</param>
    public DefaultShellProvider(IEnumerable<KeyValuePair<string, ShellConfiguration>> customShells)
        : this()
    {
        foreach (var shell in customShells)
        {
            _shells[shell.Key] = shell.Value;
        }
    }

    /// <inheritdoc />
    public ShellConfiguration? GetShellConfiguration(string shellType)
    {
        return _shells.GetValueOrDefault(shellType);
    }

    /// <inheritdoc />
    public string DefaultShellType => OperatingSystem.IsWindows() ? "cmd" : "bash";

    /// <inheritdoc />
    public IEnumerable<string> SupportedShells => _shells.Keys;
}
