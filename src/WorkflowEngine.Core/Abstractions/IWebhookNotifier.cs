using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Abstractions;

/// <summary>
/// Manages webhook notifications for workflow runs.
/// </summary>
public interface IWebhookNotifier : IAsyncDisposable
{
    /// <summary>
    /// Registers webhook configurations for a workflow run.
    /// </summary>
    /// <param name="runId">The unique identifier for the workflow run.</param>
    /// <param name="workflowName">The name of the workflow.</param>
    /// <param name="configs">The webhook configurations to register.</param>
    void RegisterWebhooks(string runId, string workflowName, IReadOnlyList<WebhookConfig> configs);

    /// <summary>
    /// Unregisters webhook configurations for a workflow run.
    /// </summary>
    /// <param name="runId">The unique identifier for the workflow run.</param>
    void UnregisterWebhooks(string runId);
}
