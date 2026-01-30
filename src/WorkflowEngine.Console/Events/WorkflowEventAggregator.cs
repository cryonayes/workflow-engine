using WorkflowEngine.Console.State;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console.Events;

/// <summary>
/// Aggregates workflow events and updates renderer state.
/// </summary>
internal sealed class WorkflowEventAggregator
{
    private readonly RendererState _state;

    /// <summary>
    /// Creates a new event aggregator.
    /// </summary>
    public WorkflowEventAggregator(RendererState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <summary>
    /// Handles workflow-level events.
    /// </summary>
    public void HandleWorkflowEvent(WorkflowEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        switch (evt)
        {
            case WorkflowStartedEvent e:
                _state.WorkflowName = e.WorkflowName;
                _state.TotalTasks = e.TotalTasks;
                _state.StartTime = e.Timestamp;
                break;

            case WaveStartedEvent e:
                UpdateWaveStatus(e.WaveIndex, WaveStatus.Running);
                break;

            case WaveCompletedEvent e:
                UpdateWaveStatus(e.WaveIndex, WaveStatus.Completed);
                break;

            case WorkflowCompletedEvent:
                _state.IsRunning = false;
                _state.IsPaused = false;
                _state.FinalDuration = DateTimeOffset.UtcNow - _state.StartTime;
                break;

            case StepPausedEvent e:
                HandleStepPaused(e);
                break;

            case StepResumedEvent:
                _state.IsPaused = false;
                _state.IsWaitingToStart = false;
                break;
        }
    }

    /// <summary>
    /// Handles task-level events.
    /// </summary>
    public void HandleTaskEvent(TaskEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        var task = _state.Tasks.FirstOrDefault(t => t.Id == evt.TaskId);
        if (task == null) return;

        switch (evt)
        {
            case TaskStartedEvent e:
                task.Status = ExecutionStatus.Running;
                task.StartTime = e.Timestamp;
                break;

            case TaskOutputEvent e:
                task.Output.Add(new OutputLine(e.Line, e.StreamType));
                break;

            case TaskCompletedEvent e:
                task.Status = e.Status;
                task.Duration = e.Duration;
                task.ExitCode = e.ExitCode;
                _state.CompletedTasks++;
                if (!e.IsSuccess) _state.FailedTasks++;
                break;

            case TaskSkippedEvent:
                task.Status = ExecutionStatus.Skipped;
                _state.CompletedTasks++;
                break;

            case TaskCancelledEvent e:
                task.Status = ExecutionStatus.Cancelled;
                task.Duration = e.Duration;
                _state.CompletedTasks++;
                _state.CancelledTasks++;
                break;
        }
    }

    private void UpdateWaveStatus(int waveIndex, WaveStatus status)
    {
        for (var i = 0; i < _state.Waves.Count; i++)
        {
            if (_state.Waves[i].Index == waveIndex)
            {
                _state.Waves[i] = _state.Waves[i] with { Status = status };
                return;
            }
        }
    }

    private void HandleStepPaused(StepPausedEvent e)
    {
        _state.IsPaused = true;
        _state.IsWaitingToStart = e.IsWaitingToStart;

        if (!e.IsWaitingToStart)
        {
            var pausedTaskIndex = _state.Tasks.FindIndex(t => t.Id == e.CompletedTaskId);
            if (pausedTaskIndex >= 0)
                _state.SelectedIndex = pausedTaskIndex;
        }
    }
}
