using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Builds docker exec commands for container execution.
/// </summary>
public interface IDockerCommandBuilder
{
    /// <summary>
    /// Builds a docker exec command.
    /// </summary>
    /// <param name="config">Docker configuration.</param>
    /// <param name="command">Command to execute inside the container.</param>
    /// <param name="environment">Environment variables to pass.</param>
    /// <param name="shell">Shell to use inside the container. If null, uses platform default from IShellProvider.</param>
    /// <returns>Executable path and arguments array.</returns>
    (string Executable, string[] Arguments) BuildCommand(
        DockerConfig config,
        string command,
        IReadOnlyDictionary<string, string> environment,
        string? shell = null);

    /// <summary>
    /// Gets the effective Docker configuration for a task.
    /// Task-level config takes precedence over workflow-level.
    /// </summary>
    /// <returns>Docker config to use, or null if Docker is disabled.</returns>
    DockerConfig? GetEffectiveConfig(Workflow workflow, WorkflowTask task);
}
