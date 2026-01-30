using System.Text;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution.Output;

namespace WorkflowEngine.Execution;

/// <summary>
/// Executes shell commands using CliWrap with real-time output streaming.
/// Uses the strategy pattern for different execution environments (Local, Docker, SSH).
/// </summary>
public sealed class ProcessExecutor : IProcessExecutor
{
    private readonly IReadOnlyList<IExecutionStrategy> _strategies;
    private readonly ITaskOutputBuilder _outputBuilder;
    private readonly ILogger<ProcessExecutor> _logger;

    /// <summary>
    /// Initializes a new instance using execution strategies.
    /// </summary>
    public ProcessExecutor(
        IEnumerable<IExecutionStrategy> strategies,
        ITaskOutputBuilder outputBuilder,
        ILogger<ProcessExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.OrderBy(s => s.Priority).ToList();
        _outputBuilder = outputBuilder ?? throw new ArgumentNullException(nameof(outputBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_strategies.Count == 0)
            throw new ArgumentException("At least one execution strategy is required", nameof(strategies));
    }

    /// <inheritdoc />
    public async Task<TaskResult> ExecuteAsync(
        string command,
        WorkflowTask task,
        WorkflowContext context,
        byte[]? input,
        IProgress<TaskProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var startTime = DateTimeOffset.UtcNow;
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var exitCode = 0;
        int? processId = null;

        try
        {
            var (executable, args, workingDir, envAction) = BuildExecutionConfig(command, task, context);

            _logger.LogDebug("Executing task '{TaskId}' via '{Executable}': {Command}", task.Id, executable,
                StringHelpers.TruncateForLog(command, TruncationLimits.CommandLog));

            // Report the command being executed
            if (context.ShowCommands)
            {
                foreach (var line in FormatCommandForDisplay(command))
                {
                    progress?.Report(new TaskProgress(task.Id, line, OutputStreamType.Command));
                }
            }

            var cmd = Cli.Wrap(executable)
                .WithArguments(args)
                .WithWorkingDirectory(workingDir)
                .WithEnvironmentVariables(envAction)
                .WithValidation(CommandResultValidation.None);

            if (input is { Length: > 0 })
            {
                cmd = cmd.WithStandardInputPipe(PipeSource.FromBytes(input));
                _logger.LogDebug("Task '{TaskId}' has {InputSize} bytes stdin", task.Id, input.Length);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeout = task.TimeoutMs ?? context.Workflow.DefaultTimeoutMs;
            timeoutCts.CancelAfter(timeout > 0 ? timeout : Defaults.TimeoutMs);

            await foreach (var evt in cmd.ListenAsync(timeoutCts.Token))
            {
                ProcessCommandEvent(evt, task.Id, stdOut, stdErr, progress, ref processId, ref exitCode);
            }

            return BuildResult(task, startTime, exitCode, stdOut, stdErr);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Task '{TaskId}' cancelled (PID: {ProcessId})", task.Id, processId);
            return TaskResult.Cancelled(task.Id, startTime);
        }
        catch (OperationCanceledException)
        {
            var timeout = TimeSpan.FromMilliseconds(task.TimeoutMs ?? context.Workflow.DefaultTimeoutMs);
            _logger.LogWarning("Task '{TaskId}' timed out after {Timeout}", task.Id, timeout);
            return TaskResult.TimedOut(task.Id, startTime, timeout, _outputBuilder.Build(task, stdOut, stdErr));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task '{TaskId}' failed", task.Id);
            return TaskResult.Failed(task.Id, startTime, ex.Message, ex);
        }
    }

    private (string Executable, string[] Args, string WorkingDir, Action<CliWrap.Builders.EnvironmentVariablesBuilder> EnvAction)
        BuildExecutionConfig(string command, WorkflowTask task, WorkflowContext context)
    {
        // Find the first strategy that can handle this task (ordered by priority)
        foreach (var strategy in _strategies)
        {
            if (strategy.CanHandle(context.Workflow, task))
            {
                _logger.LogDebug("Task '{TaskId}' using execution strategy: {Strategy}",
                    task.Id, strategy.Name);

                var config = strategy.BuildConfig(command, task, context);

                // Adapt the generic environment action to CliWrap's builder
                Action<CliWrap.Builders.EnvironmentVariablesBuilder> envAction = builder =>
                {
                    var envDict = new Dictionary<string, string>();
                    config.EnvironmentAction(envDict);
                    foreach (var (key, value) in envDict)
                        builder.Set(key, value);
                };

                return (config.Executable, config.Arguments, config.WorkingDirectory, envAction);
            }
        }

        // This should never happen if LocalExecutionStrategy is registered
        throw new InvalidOperationException(
            $"No execution strategy found for task '{task.Id}'. Ensure LocalExecutionStrategy is registered.");
    }

    private void ProcessCommandEvent(
        CommandEvent evt,
        string taskId,
        StringBuilder stdOut,
        StringBuilder stdErr,
        IProgress<TaskProgress>? progress,
        ref int? processId,
        ref int exitCode)
    {
        switch (evt)
        {
            case StartedCommandEvent started:
                processId = started.ProcessId;
                progress?.Report(new TaskProgress(taskId, $"Started (PID: {started.ProcessId})"));
                _logger.LogDebug("Task '{TaskId}' started (PID: {ProcessId})", taskId, started.ProcessId);
                break;

            case StandardOutputCommandEvent stdOutEvt:
                stdOut.AppendLine(stdOutEvt.Text);
                progress?.Report(new TaskProgress(taskId, stdOutEvt.Text, OutputStreamType.StdOut));
                break;

            case StandardErrorCommandEvent stdErrEvt:
                stdErr.AppendLine(stdErrEvt.Text);
                progress?.Report(new TaskProgress(taskId, stdErrEvt.Text, OutputStreamType.StdErr));
                break;

            case ExitedCommandEvent exited:
                exitCode = exited.ExitCode;
                _logger.LogDebug("Task '{TaskId}' exited with code {ExitCode}", taskId, exitCode);
                break;
        }
    }

    private TaskResult BuildResult(WorkflowTask task, DateTimeOffset startTime, int exitCode,
        StringBuilder stdOut, StringBuilder stdErr)
    {
        return new TaskResult
        {
            TaskId = task.Id,
            Status = exitCode == 0 ? ExecutionStatus.Succeeded : ExecutionStatus.Failed,
            ExitCode = exitCode,
            Output = _outputBuilder.Build(task, stdOut, stdErr),
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow,
            ErrorMessage = exitCode != 0
                ? StringHelpers.TruncateForLog(stdErr.ToString(), TruncationLimits.ErrorMessage)
                : null
        };
    }

    private static IEnumerable<string> FormatCommandForDisplay(string command)
    {
        var lines = command.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            yield return "$ (empty)";
            yield break;
        }

        foreach (var line in lines)
        {
            yield return $"$ {line.Trim()}";
        }
    }
}
