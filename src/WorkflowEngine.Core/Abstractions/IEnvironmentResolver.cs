namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Resolves environment variables for different execution contexts.
/// </summary>
public interface IEnvironmentResolver
{
    /// <summary>
    /// Resolves environment variables for local execution.
    /// Includes system environment variables merged with workflow/task-specific ones.
    /// </summary>
    /// <param name="workflowEnv">Environment variables from the workflow definition.</param>
    /// <param name="taskEnv">Environment variables specific to the task.</param>
    /// <param name="additionalEnv">Additional environment variables (e.g., from CLI).</param>
    /// <returns>Merged environment variables suitable for local execution.</returns>
    IReadOnlyDictionary<string, string> ResolveForLocalExecution(
        IReadOnlyDictionary<string, string>? workflowEnv = null,
        IReadOnlyDictionary<string, string>? taskEnv = null,
        IReadOnlyDictionary<string, string>? additionalEnv = null);

    /// <summary>
    /// Resolves environment variables for Docker execution.
    /// Only includes declared variables to prevent host environment leakage.
    /// </summary>
    /// <param name="workflowEnv">Environment variables from the workflow definition.</param>
    /// <param name="taskEnv">Environment variables specific to the task.</param>
    /// <param name="additionalEnv">Additional environment variables (e.g., from CLI).</param>
    /// <returns>Environment variables suitable for Docker execution.</returns>
    IReadOnlyDictionary<string, string> ResolveForDockerExecution(
        IReadOnlyDictionary<string, string>? workflowEnv = null,
        IReadOnlyDictionary<string, string>? taskEnv = null,
        IReadOnlyDictionary<string, string>? additionalEnv = null);

    /// <summary>
    /// Gets the current system environment variables.
    /// </summary>
    IReadOnlyDictionary<string, string> GetSystemEnvironment();
}
