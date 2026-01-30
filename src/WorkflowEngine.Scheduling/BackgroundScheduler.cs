using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Events;
using WorkflowEngine.Scheduling.Execution;
using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling;

/// <summary>
/// Background scheduler service that executes workflows based on cron schedules.
/// </summary>
public sealed class BackgroundScheduler : IScheduler, IAsyncDisposable
{
    private readonly IScheduleStorage _storage;
    private readonly IScheduleRunner _runner;
    private readonly ICronParser _cronParser;
    private readonly IScheduleExecutionOrchestrator _orchestrator;
    private readonly ILogger<BackgroundScheduler> _logger;
    private readonly RunningJobTracker _runningJobs;

    private Timer? _tickTimer;
    private CancellationTokenSource? _cts;
    private volatile bool _isRunning;

    private static readonly TimeSpan TickInterval = SchedulingConstants.TickInterval;

    /// <inheritdoc />
    public event EventHandler<SchedulerEvent>? OnSchedulerEvent;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Creates a new BackgroundScheduler instance.
    /// </summary>
    public BackgroundScheduler(
        IScheduleStorage storage,
        IScheduleRunner runner,
        ICronParser cronParser,
        ILogger<BackgroundScheduler> logger)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cronParser);
        ArgumentNullException.ThrowIfNull(logger);

        _storage = storage;
        _runner = runner;
        _cronParser = cronParser;
        _logger = logger;
        _runningJobs = new RunningJobTracker();

        // Create orchestrator and wire up events
        _orchestrator = new ScheduleExecutionOrchestrator(
            storage, runner, cronParser, _runningJobs,
            NullLogger<ScheduleExecutionOrchestrator>.Instance);

        if (_orchestrator is ScheduleExecutionOrchestrator concreteOrchestrator)
        {
            concreteOrchestrator.OnSchedulerEvent += (_, e) => PublishEvent(e);
        }
    }

    /// <summary>
    /// Creates a new BackgroundScheduler instance with a custom orchestrator.
    /// </summary>
    public BackgroundScheduler(
        IScheduleStorage storage,
        IScheduleRunner runner,
        ICronParser cronParser,
        IScheduleExecutionOrchestrator orchestrator,
        ILogger<BackgroundScheduler> logger)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cronParser);
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(logger);

        _storage = storage;
        _runner = runner;
        _cronParser = cronParser;
        _orchestrator = orchestrator;
        _logger = logger;
        _runningJobs = new RunningJobTracker();

        if (_orchestrator is ScheduleExecutionOrchestrator concreteOrchestrator)
        {
            concreteOrchestrator.OnSchedulerEvent += (_, e) => PublishEvent(e);
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Scheduler is already running");
            return;
        }

        _logger.LogInformation("Starting scheduler...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Initialize next run times for all enabled schedules
        var schedules = await _storage.GetEnabledAsync(cancellationToken);
        foreach (var schedule in schedules)
        {
            await UpdateNextRunTimeAsync(schedule, cancellationToken);
        }

        // Start the timer
        _tickTimer = new Timer(OnTick, null, TimeSpan.Zero, TickInterval);
        _isRunning = true;

        PublishEvent(new SchedulerStartedEvent(schedules.Count));
        _logger.LogInformation("Scheduler started with {Count} active schedules", schedules.Count);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return;

        _logger.LogInformation("Stopping scheduler...");
        _isRunning = false;

        // Stop the timer
        if (_tickTimer != null)
        {
            await _tickTimer.DisposeAsync();
            _tickTimer = null;
        }

        // Cancel all running jobs
        _runningJobs.CancelAll();

        // Wait for running tasks to complete (with timeout)
        var runningTasks = _runningJobs.GetRunningTasks();
        if (runningTasks.Length > 0)
        {
            try
            {
                await Task.WhenAny(
                    Task.WhenAll(runningTasks),
                    Task.Delay(SchedulingConstants.ShutdownTimeout, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        PublishEvent(new SchedulerStoppedEvent("Shutdown requested"));
        _logger.LogInformation("Scheduler stopped");
    }

    /// <inheritdoc />
    public async Task<WorkflowSchedule> AddScheduleAsync(
        WorkflowSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (!_cronParser.IsValid(schedule.CronExpression))
        {
            throw new ArgumentException(
                $"Invalid cron expression: {schedule.CronExpression}",
                nameof(schedule));
        }

        // Compute next run time
        var nextRun = _cronParser.GetNextOccurrence(schedule.CronExpression, DateTimeOffset.UtcNow);
        var scheduleWithNextRun = schedule.With(nextRunAt: nextRun);

        await _storage.SaveAsync(scheduleWithNextRun, cancellationToken);

        PublishEvent(new ScheduleAddedEvent(
            schedule.Id,
            schedule.WorkflowPath,
            schedule.CronExpression,
            schedule.Name));

        _logger.LogInformation(
            "Added schedule '{ScheduleId}' for '{WorkflowPath}' with cron '{Cron}' (next run: {NextRun})",
            schedule.Id, schedule.WorkflowPath, schedule.CronExpression, nextRun);

        return scheduleWithNextRun;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveScheduleAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var removed = await _storage.DeleteAsync(scheduleId, cancellationToken);

        if (removed)
        {
            // Cancel if currently running
            var cts = _runningJobs.GetCancellation(scheduleId);
            if (cts != null)
            {
                cts.Cancel();
                _runningJobs.Remove(scheduleId);
            }

            PublishEvent(new ScheduleRemovedEvent(scheduleId));
            _logger.LogInformation("Removed schedule '{ScheduleId}'", scheduleId);
        }

        return removed;
    }

    /// <inheritdoc />
    public Task<WorkflowSchedule?> GetScheduleAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        return _storage.GetAsync(scheduleId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowSchedule>> ListSchedulesAsync(
        bool enabledOnly = false,
        CancellationToken cancellationToken = default)
    {
        return enabledOnly
            ? _storage.GetEnabledAsync(cancellationToken)
            : _storage.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnableScheduleAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _storage.GetAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule not found: {scheduleId}");

        if (schedule.Enabled)
            return;

        var nextRun = _cronParser.GetNextOccurrence(schedule.CronExpression, DateTimeOffset.UtcNow);
        var updated = schedule.With(enabled: true, nextRunAt: nextRun);
        await _storage.SaveAsync(updated, cancellationToken);

        PublishEvent(new ScheduleEnabledEvent(scheduleId));
        _logger.LogInformation("Enabled schedule '{ScheduleId}'", scheduleId);
    }

    /// <inheritdoc />
    public async Task DisableScheduleAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _storage.GetAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule not found: {scheduleId}");

        if (!schedule.Enabled)
            return;

        var updated = schedule.With(enabled: false);
        await _storage.SaveAsync(updated, cancellationToken);

        PublishEvent(new ScheduleDisabledEvent(scheduleId));
        _logger.LogInformation("Disabled schedule '{ScheduleId}'", scheduleId);
    }

    /// <inheritdoc />
    public async Task<string> TriggerScheduleAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _storage.GetAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Schedule not found: {scheduleId}");

        var result = await _orchestrator.ExecuteAsync(schedule, isManual: true, cancellationToken);
        return result.RunId;
    }

    /// <inheritdoc />
    public async Task<string> DispatchAsync(ManualDispatchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _runner.DispatchAsync(request, cancellationToken);
        return result.RunId;
    }

    private async void OnTick(object? state)
    {
        if (!_isRunning || _cts?.IsCancellationRequested == true)
            return;

        try
        {
            await _orchestrator.CheckAndExecuteDueSchedulesAsync(_cts?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduler tick");
        }
    }

    private async Task UpdateNextRunTimeAsync(WorkflowSchedule schedule, CancellationToken cancellationToken)
    {
        var nextRun = _cronParser.GetNextOccurrence(schedule.CronExpression, DateTimeOffset.UtcNow);
        if (nextRun != schedule.NextRunAt)
        {
            await _storage.UpdateRunTimesAsync(
                schedule.Id,
                schedule.LastRunAt ?? DateTimeOffset.MinValue,
                nextRun,
                cancellationToken);
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _runningJobs.Dispose();
    }
}
