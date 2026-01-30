namespace WorkflowEngine.Core.Models;

/// <summary>
/// Configuration for executing a command via a specific execution strategy.
/// </summary>
/// <param name="Executable">The executable to run (e.g., "bash", "docker", "ssh").</param>
/// <param name="Arguments">Arguments to pass to the executable.</param>
/// <param name="WorkingDirectory">Working directory for local process execution.</param>
/// <param name="EnvironmentAction">Action to configure environment variables for the process.</param>
public sealed record ExecutionConfig(
    string Executable,
    string[] Arguments,
    string WorkingDirectory,
    Action<IDictionary<string, string>> EnvironmentAction);
