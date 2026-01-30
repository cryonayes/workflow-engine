using WorkflowEngine.Core.Abstractions;

namespace WorkflowEngine.Core.Models;

/// <summary>
/// Thread-safe runtime context carrying state across task executions.
/// Uses composition with specialized stores for task results, variables, and cancellations.
/// </summary>
public sealed class WorkflowContext
{
    private readonly ITaskResultStore _taskResultStore;
    private readonly IVariableStore _variableStore;
    private readonly ITaskCancellationManager _cancellationManager;
    private readonly object _statusLock = new();
    private volatile ExecutionStatus _overallStatus = ExecutionStatus.Pending;

    /// <summary>
    /// Creates a new workflow context with default stores.
    /// </summary>
    public WorkflowContext()
        : this(new TaskResultStore(), new VariableStore(), new TaskCancellationManager())
    {
    }

    /// <summary>
    /// Creates a new workflow context with custom stores (for testing/DI).
    /// </summary>
    /// <param name="taskResultStore">The task result store.</param>
    /// <param name="variableStore">The variable store.</param>
    /// <param name="cancellationManager">The cancellation manager.</param>
    public WorkflowContext(
        ITaskResultStore taskResultStore,
        IVariableStore variableStore,
        ITaskCancellationManager cancellationManager)
    {
        ArgumentNullException.ThrowIfNull(taskResultStore);
        ArgumentNullException.ThrowIfNull(variableStore);
        ArgumentNullException.ThrowIfNull(cancellationManager);

        _taskResultStore = taskResultStore;
        _variableStore = variableStore;
        _cancellationManager = cancellationManager;
    }

    /// <summary>
    /// Gets the workflow being executed.
    /// </summary>
    public required Workflow Workflow { get; init; }

    /// <summary>
    /// Gets the unique identifier for this workflow run.
    /// </summary>
    public string RunId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets when the workflow execution started.
    /// </summary>
    public DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the combined environment variables (workflow + CLI + system).
    /// Used for local execution where access to host environment is expected.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets only the explicitly declared environment variables (workflow + CLI).
    /// Does not include host system environment variables.
    /// Used for Docker execution to prevent host environment leakage.
    /// </summary>
    public IReadOnlyDictionary<string, string> DeclaredEnvironment { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the working directory for the workflow.
    /// </summary>
    public string WorkingDirectory { get; init; } = System.Environment.CurrentDirectory;

    /// <summary>
    /// Gets the cancellation token for the entire workflow.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Gets whether to show the executing command in task output.
    /// </summary>
    public bool ShowCommands { get; init; } = true;

    /// <summary>
    /// Gets the workflow parameters passed via CLI.
    /// Accessible in expressions as ${{ params.key }}.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the current overall status of the workflow.
    /// </summary>
    public ExecutionStatus OverallStatus => _overallStatus;

    /// <summary>
    /// Gets whether any task has failed.
    /// </summary>
    public bool HasFailure => _taskResultStore.HasFailure;

    /// <summary>
    /// Gets whether all completed tasks succeeded or were skipped.
    /// </summary>
    public bool AllSucceeded => _taskResultStore.AllSucceeded;

    /// <summary>
    /// Gets all recorded task results.
    /// </summary>
    public IReadOnlyDictionary<string, TaskResult> TaskResults => _taskResultStore.All;

    /// <summary>
    /// Gets the total duration of the workflow execution so far.
    /// </summary>
    public TimeSpan ElapsedTime => DateTimeOffset.UtcNow - StartTime;

    /// <summary>
    /// Records the result of a completed task.
    /// </summary>
    /// <param name="result">The task result to record.</param>
    /// <exception cref="ArgumentNullException">Thrown if result is null.</exception>
    public void RecordTaskResult(TaskResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _taskResultStore.Record(result);
        UpdateOverallStatus();
    }

    /// <summary>
    /// Gets the result of a specific task by ID.
    /// </summary>
    /// <param name="taskId">The task ID to look up.</param>
    /// <returns>The task result, or null if not found.</returns>
    public TaskResult? GetTaskResult(string taskId) => _taskResultStore.Get(taskId);

    /// <summary>
    /// Checks if all specified dependency tasks have succeeded.
    /// </summary>
    public bool DependenciesSucceeded(IEnumerable<string> dependsOn) =>
        _taskResultStore.DependenciesSucceeded(dependsOn);

    /// <summary>
    /// Checks if any specified dependency task has failed.
    /// </summary>
    public bool DependenciesFailed(IEnumerable<string> dependsOn) =>
        _taskResultStore.DependenciesFailed(dependsOn);

    /// <summary>
    /// Sets a custom workflow variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    public void SetVariable(string name, object value) => _variableStore.Set(name, value);

    /// <summary>
    /// Gets a custom workflow variable.
    /// </summary>
    /// <typeparam name="T">The expected type of the variable.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>The variable value, or default if not found or wrong type.</returns>
    public T? GetVariable<T>(string name) => _variableStore.Get<T>(name);

    /// <summary>
    /// Marks the workflow as cancelled.
    /// </summary>
    public void MarkCancelled()
    {
        lock (_statusLock)
        {
            _overallStatus = ExecutionStatus.Cancelled;
        }
    }

    /// <summary>
    /// Gets or creates a cancellation token source for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <returns>The cancellation token source for the task.</returns>
    public CancellationTokenSource GetOrCreateTaskCancellation(string taskId) =>
        _cancellationManager.GetOrCreate(taskId);

    /// <summary>
    /// Requests cancellation of a specific task.
    /// </summary>
    /// <param name="taskId">The task ID to cancel.</param>
    public void RequestTaskCancellation(string taskId) =>
        _cancellationManager.RequestCancellation(taskId);

    /// <summary>
    /// Removes the cancellation token source for a task (cleanup after execution).
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    public void RemoveTaskCancellation(string taskId) =>
        _cancellationManager.Remove(taskId);

    private void UpdateOverallStatus()
    {
        lock (_statusLock)
        {
            if (_overallStatus == ExecutionStatus.Cancelled)
                return;

            if (HasFailure)
                _overallStatus = ExecutionStatus.Failed;
            else if (AllSucceeded && _taskResultStore.Count == Workflow.Tasks.Count)
                _overallStatus = ExecutionStatus.Succeeded;
            else
                _overallStatus = ExecutionStatus.Running;
        }
    }
}
