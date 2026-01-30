using Microsoft.Extensions.Logging;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Events;
using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Execution;

/// <summary>
/// Orchestrates the execution of scheduled workflows.
/// </summary>
public sealed class ScheduleExecutionOrchestrator : IScheduleExecutionOrchestrator
{
    private readonly IScheduleStorage _storage;
    private readonly IScheduleRunner _runner;
    private readonly ICronParser _cronParser;
    private readonly RunningJobTracker _runningJobs;
    private readonly ILogger<ScheduleExecutionOrchestrator> _logger;

    /// <summary>
    /// Event raised when scheduler events occur.
    /// </summary>
    public event EventHandler<SchedulerEvent>? OnSchedulerEvent;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public ScheduleExecutionOrchestrator(
        IScheduleStorage storage,
        IScheduleRunner runner,
        ICronParser cronParser,
        RunningJobTracker runningJobs,
        ILogger<ScheduleExecutionOrchestrator> logger)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cronParser);
        ArgumentNullException.ThrowIfNull(runningJobs);
        ArgumentNullException.ThrowIfNull(logger);

        _storage = storage;
        _runner = runner;
        _cronParser = cronParser;
        _runningJobs = runningJobs;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScheduledRunResult> ExecuteAsync(
        WorkflowSchedule schedule,
        bool isManual,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var jobCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!_runningJobs.TryAdd(schedule.Id, jobCts))
        {
            if (!schedule.ExecutionPolicy.AllowOverlap)
            {
                jobCts.Dispose();
                throw new InvalidOperationException($"Schedule '{schedule.Id}' is already running");
            }
        }

        try
        {
            PublishEvent(new ScheduledRunTriggeredEvent(
                schedule.Id,
                schedule.WorkflowPath,
                Guid.NewGuid().ToString("N")[..8],
                isManual));

            var result = await _runner.RunAsync(schedule, isManual, jobCts.Token);

            // Update run times
            var nextRun = _cronParser.GetNextOccurrence(schedule.CronExpression, DateTimeOffset.UtcNow);
            await _storage.UpdateRunTimesAsync(schedule.Id, DateTimeOffset.UtcNow, nextRun, cancellationToken);

            PublishEvent(new ScheduledRunCompletedEvent(
                schedule.Id,
                result.RunId,
                result.Status,
                result.Duration ?? TimeSpan.Zero,
                result.ErrorMessage));

            return result;
        }
        finally
        {
            _runningJobs.Remove(schedule.Id);
            jobCts.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteWithTrackingAsync(
        WorkflowSchedule schedule,
        bool isManual,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(schedule, isManual, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing schedule '{ScheduleId}'", schedule.Id);
            PublishEvent(new ScheduledRunCompletedEvent(
                schedule.Id,
                Guid.NewGuid().ToString("N")[..8],
                Core.Models.ExecutionStatus.Failed,
                TimeSpan.Zero,
                ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task CheckAndExecuteDueSchedulesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var schedules = await _storage.GetEnabledAsync(cancellationToken);

        foreach (var schedule in schedules)
        {
            if (schedule.NextRunAt.HasValue && schedule.NextRunAt.Value <= now)
            {
                // Check execution policy
                if (!schedule.ExecutionPolicy.AllowOverlap && _runningJobs.IsRunning(schedule.Id))
                {
                    _logger.LogDebug(
                        "Skipping schedule '{ScheduleId}' - previous run still in progress",
                        schedule.Id);
                    continue;
                }

                // Execute in background with task tracking
                var task = ExecuteWithTrackingAsync(schedule, isManual: false, cancellationToken);
                _runningJobs.TrackTask(schedule.Id, task);
            }
        }
    }

    private void PublishEvent(SchedulerEvent evt)
    {
        try
        {
            OnSchedulerEvent?.Invoke(this, evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing scheduler event {EventType}", evt.GetType().Name);
        }
    }
}
