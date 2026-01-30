using System.Collections;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Runner;

/// <summary>
/// Factory for creating workflow execution contexts.
/// </summary>
public static class WorkflowContextFactory
{
    /// <summary>
    /// Creates a new workflow context with the specified options.
    /// </summary>
    /// <param name="workflow">The workflow being executed.</param>
    /// <param name="options">The run options.</param>
    /// <param name="cancellationToken">Cancellation token for the execution.</param>
    /// <returns>A configured workflow context.</returns>
    public static WorkflowContext Create(
        Workflow workflow,
        WorkflowRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(options);

        // Build declared environment: workflow + CLI options only (no host system vars)
        var declaredEnvironment = new Dictionary<string, string>(workflow.Environment);
        if (options.AdditionalEnvironment is not null)
        {
            foreach (var kvp in options.AdditionalEnvironment)
            {
                declaredEnvironment[kvp.Key] = kvp.Value;
            }
        }

        // Build full environment: declared + host system (for local execution)
        var fullEnvironment = new Dictionary<string, string>(declaredEnvironment);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                // Only add if not already declared (declared vars take precedence)
                fullEnvironment.TryAdd(key, value);
            }
        }

        return new WorkflowContext
        {
            Workflow = workflow,
            DeclaredEnvironment = declaredEnvironment,
            Environment = fullEnvironment,
            WorkingDirectory = workflow.WorkingDirectory ?? Environment.CurrentDirectory,
            CancellationToken = cancellationToken,
            ShowCommands = options.ShowCommands,
            Parameters = options.Parameters ?? new Dictionary<string, string>()
        };
    }
}
