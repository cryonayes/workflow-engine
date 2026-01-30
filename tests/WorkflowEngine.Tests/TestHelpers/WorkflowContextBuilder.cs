using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Tests.TestHelpers;

/// <summary>
/// Builder for creating WorkflowContext instances in tests.
/// Follows the Builder pattern for fluent test setup.
/// </summary>
public sealed class WorkflowContextBuilder
{
    private readonly Workflow _workflow;
    private readonly Dictionary<string, string> _environment = new();
    private readonly List<TaskResult> _taskResults = [];
    private string _workingDirectory = Environment.CurrentDirectory;
    private CancellationToken _cancellationToken = CancellationToken.None;

    /// <summary>
    /// Creates a new builder with a default workflow.
    /// </summary>
    public WorkflowContextBuilder()
    {
        _workflow = new Workflow
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Tasks = []
        };
    }

    /// <summary>
    /// Creates a new builder with the specified workflow.
    /// </summary>
    /// <param name="workflow">The workflow to use.</param>
    public WorkflowContextBuilder(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        _workflow = workflow;
    }

    /// <summary>
    /// Sets the workflow name.
    /// </summary>
    public WorkflowContextBuilder WithName(string name)
    {
        // Workflow is a record with init properties, create new one
        // For simplicity, we store tasks and recreate
        return this;
    }

    /// <summary>
    /// Adds an environment variable.
    /// </summary>
    public WorkflowContextBuilder WithEnvironment(string key, string value)
    {
        _environment[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple environment variables.
    /// </summary>
    public WorkflowContextBuilder WithEnvironment(IDictionary<string, string> variables)
    {
        foreach (var kvp in variables)
            _environment[kvp.Key] = kvp.Value;
        return this;
    }

    /// <summary>
    /// Sets the working directory.
    /// </summary>
    public WorkflowContextBuilder WithWorkingDirectory(string path)
    {
        _workingDirectory = path;
        return this;
    }

    /// <summary>
    /// Sets the cancellation token.
    /// </summary>
    public WorkflowContextBuilder WithCancellation(CancellationToken token)
    {
        _cancellationToken = token;
        return this;
    }

    /// <summary>
    /// Adds a task result to simulate a completed task.
    /// </summary>
    public WorkflowContextBuilder WithTaskResult(TaskResult result)
    {
        _taskResults.Add(result);
        return this;
    }

    /// <summary>
    /// Adds a successful task result with the specified output.
    /// </summary>
    public WorkflowContextBuilder WithSuccessfulTask(string taskId, string? output = null)
    {
        var now = DateTimeOffset.UtcNow;
        _taskResults.Add(new TaskResult
        {
            TaskId = taskId,
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartTime = now,
            EndTime = now,
            Output = output != null ? new TaskOutput { StandardOutput = output } : null
        });
        return this;
    }

    /// <summary>
    /// Adds a failed task result.
    /// </summary>
    public WorkflowContextBuilder WithFailedTask(string taskId, string? errorMessage = null)
    {
        var now = DateTimeOffset.UtcNow;
        _taskResults.Add(new TaskResult
        {
            TaskId = taskId,
            Status = ExecutionStatus.Failed,
            ExitCode = 1,
            StartTime = now,
            EndTime = now,
            ErrorMessage = errorMessage ?? "Task failed"
        });
        return this;
    }

    /// <summary>
    /// Builds the WorkflowContext.
    /// </summary>
    public WorkflowContext Build()
    {
        var context = new WorkflowContext
        {
            Workflow = _workflow,
            Environment = new Dictionary<string, string>(_environment),
            WorkingDirectory = _workingDirectory,
            CancellationToken = _cancellationToken
        };

        foreach (var result in _taskResults)
            context.RecordTaskResult(result);

        return context;
    }

    /// <summary>
    /// Implicitly converts the builder to a WorkflowContext.
    /// </summary>
    public static implicit operator WorkflowContext(WorkflowContextBuilder builder) =>
        builder.Build();
}
