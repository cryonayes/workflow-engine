using Microsoft.Extensions.Logging;
using WorkflowEngine.Core;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution;

/// <summary>
/// Executes individual workflow tasks with condition evaluation, input preparation, and retry logic.
/// </summary>
public sealed class TaskExecutor : ITaskExecutor
{
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IExpressionInterpolator _interpolator;
    private readonly IProcessExecutor _processExecutor;
    private readonly ITaskInputResolver _inputResolver;
    private readonly IDockerCommandBuilder _dockerBuilder;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<TaskExecutor> _logger;

    /// <summary>
    /// Initializes a new instance with all dependencies.
    /// </summary>
    public TaskExecutor(
        IConditionEvaluator conditionEvaluator,
        IExpressionInterpolator interpolator,
        IProcessExecutor processExecutor,
        ITaskInputResolver inputResolver,
        IDockerCommandBuilder dockerBuilder,
        IRetryPolicy retryPolicy,
        ILogger<TaskExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(conditionEvaluator);
        ArgumentNullException.ThrowIfNull(interpolator);
        ArgumentNullException.ThrowIfNull(processExecutor);
        ArgumentNullException.ThrowIfNull(inputResolver);
        ArgumentNullException.ThrowIfNull(dockerBuilder);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(logger);

        _conditionEvaluator = conditionEvaluator;
        _interpolator = interpolator;
        _processExecutor = processExecutor;
        _inputResolver = inputResolver;
        _dockerBuilder = dockerBuilder;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TaskResult> ExecuteAsync(
        WorkflowTask task,
        WorkflowContext context,
        IProgress<TaskProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var startTime = DateTimeOffset.UtcNow;
        _logger.LogDebug("Starting execution of task '{TaskId}'", task.Id);

        try
        {
            // Evaluate condition if present
            if (!ShouldExecute(task, context))
            {
                var reason = GetSkipReason(task, context);
                _logger.LogInformation("Skipping task '{TaskId}': {Reason}", task.Id, reason);
                return TaskResult.Skipped(task.Id, reason);
            }

            // Determine execution environment based on Docker configuration
            var isDockerExecution = _dockerBuilder.GetEffectiveConfig(context.Workflow, task) is not null;

            // Interpolate variables in the command
            // For Docker execution, use only declared environment to prevent host env leakage
            // For local execution, use full environment including host system vars
            var interpolationEnv = isDockerExecution ? context.DeclaredEnvironment : context.Environment;
            var command = _interpolator.Interpolate(task.Run, context, interpolationEnv);
            _logger.LogDebug("Task '{TaskId}' interpolated command (docker={IsDocker}): {Command}",
                task.Id, isDockerExecution, StringHelpers.TruncateForLog(command, TruncationLimits.CommandLog));

            // Prepare input
            var input = await _inputResolver.ResolveInputAsync(task, context, cancellationToken);

            // Execute with retry logic
            return await ExecuteWithRetryAsync(task, context, command, input, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Task '{TaskId}' execution was cancelled", task.Id);
            return TaskResult.Cancelled(task.Id, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task '{TaskId}' failed with unexpected exception", task.Id);
            return TaskResult.Failed(task.Id, startTime, ex.Message, ex);
        }
    }

    private bool ShouldExecute(WorkflowTask task, WorkflowContext context) =>
        task.If is not null
            ? _conditionEvaluator.EvaluateCondition(task.If, context, task.DependsOn)
            : task.DependsOn.Count == 0 || context.DependenciesSucceeded(task.DependsOn);

    private static string GetSkipReason(WorkflowTask task, WorkflowContext context)
    {
        if (task.If is not null)
            return $"Condition '{task.If}' evaluated to false";

        var failedDeps = task.DependsOn.Where(d => context.GetTaskResult(d)?.IsSuccess != true);
        return failedDeps.Any()
            ? $"Dependencies failed: {string.Join(", ", failedDeps)}"
            : "Unknown reason";
    }

    private async Task<TaskResult> ExecuteWithRetryAsync(
        WorkflowTask task,
        WorkflowContext context,
        string command,
        byte[]? input,
        IProgress<TaskProgress>? progress,
        CancellationToken cancellationToken)
    {
        var settings = RetrySettings.FromTask(task);

        return await _retryPolicy.ExecuteAsync(
            async ct => await _processExecutor.ExecuteAsync(command, task, context, input, progress, ct),
            settings,
            onRetry: (attempt, _) =>
            {
                _logger.LogInformation(
                    "Retrying task '{TaskId}' (attempt {Attempt}/{MaxAttempts})",
                    task.Id, attempt + 1, task.RetryCount + 1);
                progress?.Report(new TaskProgress(task.Id, $"Retry attempt {attempt}/{task.RetryCount}"));
            },
            cancellationToken);
    }

}
