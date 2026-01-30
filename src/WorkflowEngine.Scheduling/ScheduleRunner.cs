using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling;

/// <summary>
/// Executes scheduled workflows by bridging to IWorkflowRunner.
/// </summary>
public sealed class ScheduleRunner : IScheduleRunner
{
    private readonly IWorkflowParser _parser;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly ILogger<ScheduleRunner> _logger;

    /// <summary>
    /// Creates a new ScheduleRunner instance.
    /// </summary>
    public ScheduleRunner(
        IWorkflowParser parser,
        IWorkflowRunner workflowRunner,
        ILogger<ScheduleRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(workflowRunner);
        ArgumentNullException.ThrowIfNull(logger);

        _parser = parser;
        _workflowRunner = workflowRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScheduledRunResult> RunAsync(
        WorkflowSchedule schedule,
        bool isManual = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var scheduledTime = schedule.NextRunAt ?? DateTimeOffset.UtcNow;
        var startTime = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "Starting scheduled run for '{WorkflowPath}' (schedule: {ScheduleId}, run: {RunId}, manual: {IsManual})",
            schedule.WorkflowPath, schedule.Id, runId, isManual);

        try
        {
            var workflow = _parser.ParseFile(schedule.WorkflowPath);

            var options = new WorkflowRunOptions
            {
                AdditionalEnvironment = schedule.InputParameters.ToDictionary(k => k.Key, v => v.Value)
            };

            var context = await _workflowRunner.RunAsync(workflow, options, cancellationToken);

            _logger.LogInformation(
                "Scheduled run completed for '{WorkflowPath}' (schedule: {ScheduleId}, status: {Status})",
                schedule.WorkflowPath, schedule.Id, context.OverallStatus);

            return new ScheduledRunResult
            {
                ScheduleId = schedule.Id,
                RunId = context.RunId,
                ScheduledTime = scheduledTime,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Status = context.OverallStatus,
                IsManualTrigger = isManual
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Scheduled run failed for '{WorkflowPath}' (schedule: {ScheduleId})",
                schedule.WorkflowPath, schedule.Id);

            return new ScheduledRunResult
            {
                ScheduleId = schedule.Id,
                RunId = runId,
                ScheduledTime = scheduledTime,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Status = ExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                IsManualTrigger = isManual
            };
        }
    }

    /// <inheritdoc />
    public async Task<ScheduledRunResult> DispatchAsync(
        ManualDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Create a temporary schedule for dispatch
        var tempSchedule = new WorkflowSchedule
        {
            Id = $"dispatch-{Guid.NewGuid():N}"[..16],
            WorkflowPath = request.WorkflowPath,
            CronExpression = "* * * * *", // Not used for dispatch
            Name = $"Manual dispatch: {request.Reason ?? "N/A"}",
            InputParameters = request.InputParameters,
            NextRunAt = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "Dispatching workflow '{WorkflowPath}' (reason: {Reason}, triggered by: {TriggeredBy})",
            request.WorkflowPath, request.Reason, request.TriggeredBy);

        return await RunAsync(tempSchedule, isManual: true, cancellationToken);
    }
}
