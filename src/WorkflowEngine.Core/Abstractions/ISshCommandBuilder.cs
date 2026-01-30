using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Builds SSH commands for remote execution.
/// </summary>
public interface ISshCommandBuilder
{
    /// <summary>
    /// Builds an SSH command for remote execution.
    /// </summary>
    /// <param name="config">SSH configuration.</param>
    /// <param name="command">Command to execute on the remote host.</param>
    /// <param name="environment">Environment variables to set.</param>
    /// <param name="shell">Shell to use on the remote host. If null, uses platform default.</param>
    /// <returns>Executable path and arguments array.</returns>
    (string Executable, string[] Arguments) BuildCommand(
        SshConfig config,
        string command,
        IReadOnlyDictionary<string, string> environment,
        string? shell = null);

    /// <summary>
    /// Gets the effective SSH configuration for a task.
    /// Task-level config takes precedence over workflow-level.
    /// </summary>
    /// <param name="workflow">The workflow definition.</param>
    /// <param name="task">The task to get configuration for.</param>
    /// <returns>SSH config to use, or null if SSH is disabled.</returns>
    SshConfig? GetEffectiveConfig(Workflow workflow, WorkflowTask task);
}
