using FluentAssertions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Tests.Core;

public class WorkflowContextTests
{
    [Fact]
    public void RecordTaskResult_AddsResultToContext()
    {
        // Arrange
        var context = CreateContext();
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0
        };

        // Act
        context.RecordTaskResult(result);

        // Assert
        context.TaskResults.Should().ContainKey("task1");
        context.TaskResults["task1"].Should().Be(result);
    }

    [Fact]
    public void GetTaskResult_ReturnsResult_WhenResultExists()
    {
        // Arrange
        var context = CreateContext();
        var result = new TaskResult
        {
            TaskId = "task1",
            Status = ExecutionStatus.Succeeded
        };
        context.RecordTaskResult(result);

        // Act
        var retrieved = context.GetTaskResult("task1");

        // Assert
        retrieved.Should().Be(result);
    }

    [Fact]
    public void GetTaskResult_ReturnsNull_WhenResultNotExists()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var retrieved = context.GetTaskResult("nonexistent");

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public void OverallStatus_ReturnsSucceeded_WhenAllTasksSucceeded()
    {
        // Arrange
        var context = CreateContextWithTasks("t1", "t2");
        context.RecordTaskResult(new TaskResult { TaskId = "t1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "t2", Status = ExecutionStatus.Succeeded });

        // Act & Assert
        context.OverallStatus.Should().Be(ExecutionStatus.Succeeded);
    }

    [Fact]
    public void OverallStatus_ReturnsFailed_WhenAnyTaskFailed()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "t1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "t2", Status = ExecutionStatus.Failed });

        // Act & Assert
        context.OverallStatus.Should().Be(ExecutionStatus.Failed);
    }

    [Fact]
    public void OverallStatus_ReturnsPending_WhenNoTasksCompleted()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        context.OverallStatus.Should().Be(ExecutionStatus.Pending);
    }

    [Fact]
    public void MarkCancelled_SetsStatusToCancelled()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.MarkCancelled();

        // Assert
        context.OverallStatus.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public void OverallStatus_ReturnsCancelled_WhenMarkedCancelledAfterSuccess()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "t1", Status = ExecutionStatus.Succeeded });
        context.MarkCancelled();

        // Act & Assert
        context.OverallStatus.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public void RunId_IsGeneratedOnCreation()
    {
        // Arrange & Act
        var context = CreateContext();

        // Assert
        context.RunId.Should().NotBeEmpty();
    }

    [Fact]
    public void RecordTaskResult_OverwritesExistingResult()
    {
        // Arrange
        var context = CreateContext();
        var firstResult = new TaskResult { TaskId = "task1", Status = ExecutionStatus.Running };
        var secondResult = new TaskResult { TaskId = "task1", Status = ExecutionStatus.Succeeded };

        // Act
        context.RecordTaskResult(firstResult);
        context.RecordTaskResult(secondResult);

        // Assert
        context.TaskResults["task1"].Status.Should().Be(ExecutionStatus.Succeeded);
    }

    [Fact]
    public void DependenciesSucceeded_ReturnsTrue_WhenAllDependenciesSucceeded()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "dep1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "dep2", Status = ExecutionStatus.Succeeded });

        // Act
        var result = context.DependenciesSucceeded(["dep1", "dep2"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DependenciesSucceeded_ReturnsFalse_WhenAnyDependencyFailed()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "dep1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "dep2", Status = ExecutionStatus.Failed });

        // Act
        var result = context.DependenciesSucceeded(["dep1", "dep2"]);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DependenciesFailed_ReturnsTrue_WhenAnyDependencyFailed()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "dep1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "dep2", Status = ExecutionStatus.Failed });

        // Act
        var result = context.DependenciesFailed(["dep1", "dep2"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DependenciesFailed_ReturnsFalse_WhenAllDependenciesSucceeded()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "dep1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "dep2", Status = ExecutionStatus.Succeeded });

        // Act
        var result = context.DependenciesFailed(["dep1", "dep2"]);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DependenciesSucceeded_ReturnsFalse_WhenDependencyIsCancelled()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "dep1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "dep2", Status = ExecutionStatus.Cancelled });

        // Act
        var result = context.DependenciesSucceeded(["dep1", "dep2"]);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DependenciesFailed_ReturnsTrue_WhenDependencyIsCancelled()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "dep1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "dep2", Status = ExecutionStatus.Cancelled });

        // Act
        var result = context.DependenciesFailed(["dep1", "dep2"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasFailure_ReturnsTrue_WhenAnyTaskFailed()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "t1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "t2", Status = ExecutionStatus.Failed });

        // Act & Assert
        context.HasFailure.Should().BeTrue();
    }

    [Fact]
    public void HasFailure_ReturnsTrue_WhenAnyTaskCancelled()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "t1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "t2", Status = ExecutionStatus.Cancelled });

        // Act & Assert
        context.HasFailure.Should().BeTrue();
    }

    [Fact]
    public void AllSucceeded_ReturnsTrue_WhenAllTasksSucceededOrSkipped()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult { TaskId = "t1", Status = ExecutionStatus.Succeeded });
        context.RecordTaskResult(new TaskResult { TaskId = "t2", Status = ExecutionStatus.Skipped });

        // Act & Assert
        context.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public void SetVariable_And_GetVariable_WorksCorrectly()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.SetVariable("myVar", "myValue");
        var result = context.GetVariable<string>("myVar");

        // Assert
        result.Should().Be("myValue");
    }

    [Fact]
    public void GetVariable_ReturnsDefault_WhenVariableNotFound()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.GetVariable<string>("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOrCreateTaskCancellation_CreatesCancellationTokenSource()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var cts = context.GetOrCreateTaskCancellation("task1");

        // Assert
        cts.Should().NotBeNull();
        cts.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void GetOrCreateTaskCancellation_ReturnsSameInstance_ForSameTaskId()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var cts1 = context.GetOrCreateTaskCancellation("task1");
        var cts2 = context.GetOrCreateTaskCancellation("task1");

        // Assert
        cts1.Should().BeSameAs(cts2);
    }

    [Fact]
    public void RequestTaskCancellation_CancelsTask()
    {
        // Arrange
        var context = CreateContext();
        var cts = context.GetOrCreateTaskCancellation("task1");

        // Act
        context.RequestTaskCancellation("task1");

        // Assert
        cts.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void RequestTaskCancellation_DoesNotThrow_WhenTaskNotRegistered()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var act = () => context.RequestTaskCancellation("nonexistent");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveTaskCancellation_RemovesCancellationTokenSource()
    {
        // Arrange
        var context = CreateContext();
        var originalCts = context.GetOrCreateTaskCancellation("task1");

        // Act
        context.RemoveTaskCancellation("task1");
        var newCts = context.GetOrCreateTaskCancellation("task1");

        // Assert
        newCts.Should().NotBeSameAs(originalCts);
    }

    [Fact]
    public void RemoveTaskCancellation_DoesNotThrow_WhenTaskNotRegistered()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var act = () => context.RemoveTaskCancellation("nonexistent");

        // Assert
        act.Should().NotThrow();
    }

    private static WorkflowContext CreateContext()
    {
        return new WorkflowContext
        {
            Workflow = new Workflow
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                Tasks = []
            },
            Environment = new Dictionary<string, string>(),
            WorkingDirectory = Environment.CurrentDirectory
        };
    }

    private static WorkflowContext CreateContextWithTasks(params string[] taskIds)
    {
        var tasks = taskIds.Select(id => new WorkflowTask { Id = id, Run = "echo test" }).ToList();
        return new WorkflowContext
        {
            Workflow = new Workflow
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                Tasks = tasks
            },
            Environment = new Dictionary<string, string>(),
            WorkingDirectory = Environment.CurrentDirectory
        };
    }
}
