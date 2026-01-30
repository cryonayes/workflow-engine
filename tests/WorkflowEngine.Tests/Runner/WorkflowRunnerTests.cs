using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Events;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner;
using WorkflowEngine.Runner.Events;
using WorkflowEngine.Runner.Execution;
using WorkflowEngine.Runner.Matrix;
using WorkflowEngine.Runner.StepMode;

namespace WorkflowEngine.Tests.Runner;

public class WorkflowRunnerTests
{
    private readonly ITaskExecutor _taskExecutor;
    private readonly IExecutionScheduler _scheduler;
    private readonly IEventPublisher _eventPublisher;
    private readonly IStepModeHandler _stepModeHandler;
    private readonly IWaveExecutor _waveExecutor;
    private readonly WorkflowRunner _runner;

    public WorkflowRunnerTests()
    {
        _taskExecutor = Substitute.For<ITaskExecutor>();
        _scheduler = CreateDagScheduler();
        _eventPublisher = new WorkflowEventPublisher(NullLogger<WorkflowEventPublisher>.Instance);
        _stepModeHandler = new StepModeHandler(_eventPublisher, NullLogger<StepModeHandler>.Instance);
        _waveExecutor = new WaveExecutor(
            _taskExecutor,
            _eventPublisher,
            _stepModeHandler,
            NullLogger<WaveExecutor>.Instance);
        _runner = new WorkflowRunner(
            _scheduler,
            _waveExecutor,
            _stepModeHandler,
            _eventPublisher,
            NullLogger<WorkflowRunner>.Instance);
    }

    [Fact]
    public async Task RunAsync_WithStepMode_ExecutesTasksSequentially()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "task1", Run = "echo 1" },
            new WorkflowTask { Id = "task2", Run = "echo 2" }
        );

        var executionOrder = new List<string>();
        _taskExecutor.ExecuteAsync(Arg.Any<WorkflowTask>(), Arg.Any<WorkflowContext>(),
            Arg.Any<IProgress<TaskProgress>>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var task = x.Arg<WorkflowTask>();
                executionOrder.Add(task.Id);
                return Task.FromResult(CreateSuccessResult(task.Id));
            });

        var stepController = new AutoContinueStepController();
        var options = new WorkflowRunOptions
        {
            StepMode = true,
            StepController = stepController
        };

        // Act
        await _runner.RunAsync(workflow, options);

        // Assert - tasks should have executed in order
        executionOrder.Should().BeEquivalentTo(["task1", "task2"]);
        // Paused twice: once before start, once after task1 (not after last task)
        stepController.PauseCount.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_WithStepMode_PublishesStepPausedEvent()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "task1", Run = "echo 1" },
            new WorkflowTask { Id = "task2", Run = "echo 2" }
        );

        _taskExecutor.ExecuteAsync(Arg.Any<WorkflowTask>(), Arg.Any<WorkflowContext>(),
            Arg.Any<IProgress<TaskProgress>>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var task = x.Arg<WorkflowTask>();
                return Task.FromResult(CreateSuccessResult(task.Id));
            });

        var stepController = new AutoContinueStepController();
        var options = new WorkflowRunOptions
        {
            StepMode = true,
            StepController = stepController
        };

        var pausedEvents = new List<StepPausedEvent>();
        _runner.OnWorkflowEvent += (_, e) =>
        {
            if (e is StepPausedEvent pause)
                pausedEvents.Add(pause);
        };

        // Act
        await _runner.RunAsync(workflow, options);

        // Assert - two pauses: one before start, one after task1
        pausedEvents.Should().HaveCount(2);
        pausedEvents[0].CompletedTaskId.Should().BeEmpty();
        pausedEvents[0].IsWaitingToStart.Should().BeTrue();
        pausedEvents[1].CompletedTaskId.Should().Be("task1");
        pausedEvents[1].IsWaitingToStart.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WithStepMode_WaveCompletedBeforePauseBetweenWaves()
    {
        // Arrange - tasks with dependencies create separate waves
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "task1", Run = "echo 1" },
            new WorkflowTask { Id = "task2", Run = "echo 2", DependsOn = ["task1"] }
        );

        _taskExecutor.ExecuteAsync(Arg.Any<WorkflowTask>(), Arg.Any<WorkflowContext>(),
            Arg.Any<IProgress<TaskProgress>>(), Arg.Any<CancellationToken>())
            .Returns(x => Task.FromResult(CreateSuccessResult(x.Arg<WorkflowTask>().Id)));

        var stepController = new AutoContinueStepController();
        var options = new WorkflowRunOptions
        {
            StepMode = true,
            StepController = stepController
        };

        // Capture event order with unique identifiers
        var eventOrder = new List<string>();
        _runner.OnWorkflowEvent += (_, e) =>
        {
            var name = e switch
            {
                WaveCompletedEvent wc => $"WaveCompleted({wc.WaveIndex})",
                WaveStartedEvent ws => $"WaveStarted({ws.WaveIndex})",
                StepPausedEvent sp => sp.IsWaitingToStart ? "StepPaused(start)" : $"StepPaused({sp.CompletedTaskId})",
                _ => e.GetType().Name
            };
            eventOrder.Add(name);
        };

        // Act
        await _runner.RunAsync(workflow, options);

        // Assert - WaveCompleted(0) must come BEFORE StepPaused(task1) which is the pause between waves
        var waveCompleted0Index = eventOrder.FindIndex(e => e == "WaveCompleted(0)");
        var pauseAfterWave0Index = eventOrder.FindIndex(e => e == "StepPaused(task1)");
        var waveStarted1Index = eventOrder.FindIndex(e => e == "WaveStarted(1)");

        waveCompleted0Index.Should().BeGreaterThan(-1, "WaveCompleted(0) should be published");
        pauseAfterWave0Index.Should().BeGreaterThan(waveCompleted0Index,
            "Pause after task1 should happen AFTER WaveCompleted(0)");
        waveStarted1Index.Should().BeGreaterThan(pauseAfterWave0Index,
            "WaveStarted(1) should happen AFTER pause");
    }

    [Fact]
    public async Task RunAsync_WithStepMode_MultipleTasksInWave_WaveCompletedAfterLastTask()
    {
        // Arrange - 3 independent tasks in one wave (no dependencies = parallel = same wave)
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "task1", Run = "echo 1" },
            new WorkflowTask { Id = "task2", Run = "echo 2" },
            new WorkflowTask { Id = "task3", Run = "echo 3" }
        );

        _taskExecutor.ExecuteAsync(Arg.Any<WorkflowTask>(), Arg.Any<WorkflowContext>(),
            Arg.Any<IProgress<TaskProgress>>(), Arg.Any<CancellationToken>())
            .Returns(x => Task.FromResult(CreateSuccessResult(x.Arg<WorkflowTask>().Id)));

        var stepController = new AutoContinueStepController();
        var options = new WorkflowRunOptions
        {
            StepMode = true,
            StepController = stepController
        };

        var eventOrder = new List<string>();
        _runner.OnWorkflowEvent += (_, e) =>
        {
            var name = e switch
            {
                WaveCompletedEvent wc => $"WaveCompleted({wc.WaveIndex})",
                WaveStartedEvent ws => $"WaveStarted({ws.WaveIndex})",
                StepPausedEvent sp => sp.IsWaitingToStart
                    ? "StepPaused(start)"
                    : $"StepPaused({sp.CompletedTaskId})",
                _ => e.GetType().Name
            };
            eventOrder.Add(name);
        };

        // Act
        await _runner.RunAsync(workflow, options);

        // Assert - Wave should be marked completed only after the last task
        // Expected order: StepPaused(start), WaveStarted(0),
        //                 [task1], StepPaused(task1),
        //                 [task2], StepPaused(task2),
        //                 [task3], WaveCompleted(0), WorkflowCompleted
        var pauseTask1 = eventOrder.FindIndex(e => e == "StepPaused(task1)");
        var pauseTask2 = eventOrder.FindIndex(e => e == "StepPaused(task2)");
        var waveCompleted = eventOrder.FindIndex(e => e == "WaveCompleted(0)");

        // Verify pauses happen within the wave (wave not completed yet)
        pauseTask1.Should().BeLessThan(waveCompleted, "pause after task1 should be before wave completion");
        pauseTask2.Should().BeLessThan(waveCompleted, "pause after task2 should be before wave completion");

        // No pause after task3 (last task in wave) - wave completes directly
        eventOrder.Should().NotContain("StepPaused(task3)",
            "no pause after last task in wave - wave completes instead");
    }

    [Fact]
    public async Task RunAsync_WithoutStepMode_ExecutesTasksInParallel()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "task1", Run = "echo 1" },
            new WorkflowTask { Id = "task2", Run = "echo 2" }
        );

        var startTimes = new Dictionary<string, DateTimeOffset>();
        _taskExecutor.ExecuteAsync(Arg.Any<WorkflowTask>(), Arg.Any<WorkflowContext>(),
            Arg.Any<IProgress<TaskProgress>>(), Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                var task = x.Arg<WorkflowTask>();
                startTimes[task.Id] = DateTimeOffset.UtcNow;
                await Task.Delay(50); // Small delay to ensure overlap detection
                return CreateSuccessResult(task.Id);
            });

        var options = new WorkflowRunOptions { StepMode = false };

        // Act
        await _runner.RunAsync(workflow, options);

        // Assert - both tasks should start within a small window (parallel)
        var timeDiff = Math.Abs((startTimes["task1"] - startTimes["task2"]).TotalMilliseconds);
        timeDiff.Should().BeLessThan(40); // Should start almost simultaneously
    }

    private static DagScheduler CreateDagScheduler()
    {
        var combinationGenerator = new MatrixCombinationGenerator();
        var interpolator = new MatrixExpressionInterpolator();
        var taskBuilder = new ExpandedTaskBuilder(interpolator);
        var dependencyRewriter = new DependencyRewriter(taskBuilder);
        var matrixExpander = new MatrixExpander(combinationGenerator, taskBuilder, dependencyRewriter);
        return new DagScheduler(matrixExpander);
    }

    private static Workflow CreateWorkflow(params WorkflowTask[] tasks)
    {
        return new Workflow
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Tasks = tasks.ToList()
        };
    }

    private static TaskResult CreateSuccessResult(string taskId)
    {
        var now = DateTimeOffset.UtcNow;
        return new TaskResult
        {
            TaskId = taskId,
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartTime = now,
            EndTime = now
        };
    }

    /// <summary>
    /// Step controller that automatically continues after each pause.
    /// </summary>
    private sealed class AutoContinueStepController : IStepController
    {
        private int _pauseCount;

        public int PauseCount => _pauseCount;

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _pauseCount);
            return Task.CompletedTask;
        }

        public void Release()
        {
            // No-op, auto-continues
        }
    }
}
