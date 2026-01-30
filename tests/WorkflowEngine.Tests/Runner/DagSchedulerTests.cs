using FluentAssertions;
using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner;
using WorkflowEngine.Runner.Matrix;

namespace WorkflowEngine.Tests.Runner;

public class DagSchedulerTests
{
    private readonly DagScheduler _scheduler;

    public DagSchedulerTests()
    {
        var combinationGenerator = new MatrixCombinationGenerator();
        var interpolator = new MatrixExpressionInterpolator();
        var taskBuilder = new ExpandedTaskBuilder(interpolator);
        var dependencyRewriter = new DependencyRewriter(taskBuilder);
        var matrixExpander = new MatrixExpander(combinationGenerator, taskBuilder, dependencyRewriter);
        _scheduler = new DagScheduler(matrixExpander);
    }

    [Fact]
    public void BuildExecutionPlan_WithNoDependencies_CreatesParallelWave()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "task1", Run = "echo 1" },
            new WorkflowTask { Id = "task2", Run = "echo 2" },
            new WorkflowTask { Id = "task3", Run = "echo 3" }
        );

        // Act
        var plan = _scheduler.BuildExecutionPlan(workflow);

        // Assert
        plan.Waves.Should().HaveCount(1);
        plan.Waves[0].Tasks.Should().HaveCount(3);
        plan.TotalTasks.Should().Be(3);
    }

    [Fact]
    public void BuildExecutionPlan_WithSequentialDependencies_CreatesOrderedWaves()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "first", Run = "echo first" },
            new WorkflowTask { Id = "second", Run = "echo second", DependsOn = ["first"] },
            new WorkflowTask { Id = "third", Run = "echo third", DependsOn = ["second"] }
        );

        // Act
        var plan = _scheduler.BuildExecutionPlan(workflow);

        // Assert
        plan.Waves.Should().HaveCount(3);
        plan.Waves[0].Tasks.Single().Id.Should().Be("first");
        plan.Waves[1].Tasks.Single().Id.Should().Be("second");
        plan.Waves[2].Tasks.Single().Id.Should().Be("third");
    }

    [Fact]
    public void BuildExecutionPlan_WithParallelBranches_GroupsTasksInSameWave()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "root", Run = "echo root" },
            new WorkflowTask { Id = "branch1", Run = "echo branch1", DependsOn = ["root"] },
            new WorkflowTask { Id = "branch2", Run = "echo branch2", DependsOn = ["root"] },
            new WorkflowTask { Id = "merge", Run = "echo merge", DependsOn = ["branch1", "branch2"] }
        );

        // Act
        var plan = _scheduler.BuildExecutionPlan(workflow);

        // Assert
        plan.Waves.Should().HaveCount(3);
        plan.Waves[0].Tasks.Single().Id.Should().Be("root");
        plan.Waves[1].Tasks.Should().HaveCount(2);
        plan.Waves[1].Tasks.Select(t => t.Id).Should().BeEquivalentTo(["branch1", "branch2"]);
        plan.Waves[2].Tasks.Single().Id.Should().Be("merge");
    }

    [Fact]
    public void BuildExecutionPlan_WithAlwaysCondition_SeparatesAlwaysTasks()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "main", Run = "echo main" },
            new WorkflowTask { Id = "cleanup", Run = "echo cleanup", If = "${{ always() }}" }
        );

        // Act
        var plan = _scheduler.BuildExecutionPlan(workflow);

        // Assert
        plan.AlwaysTasks.Should().HaveCount(1);
        plan.AlwaysTasks[0].Id.Should().Be("cleanup");
        plan.Waves.Should().HaveCount(1);
        plan.Waves[0].Tasks.Single().Id.Should().Be("main");
    }

    [Fact]
    public void BuildExecutionPlan_WithCircularDependency_ThrowsException()
    {
        // Arrange
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "a", Run = "echo a", DependsOn = ["c"] },
            new WorkflowTask { Id = "b", Run = "echo b", DependsOn = ["a"] },
            new WorkflowTask { Id = "c", Run = "echo c", DependsOn = ["b"] }
        );

        // Act & Assert
        var act = () => _scheduler.BuildExecutionPlan(workflow);
        act.Should().Throw<CircularDependencyException>();
    }

    [Fact]
    public void BuildExecutionPlan_WithDiamondDependency_ResolvesCorrectly()
    {
        // Arrange: Diamond pattern A -> B, C -> D
        var workflow = CreateWorkflow(
            new WorkflowTask { Id = "a", Run = "echo a" },
            new WorkflowTask { Id = "b", Run = "echo b", DependsOn = ["a"] },
            new WorkflowTask { Id = "c", Run = "echo c", DependsOn = ["a"] },
            new WorkflowTask { Id = "d", Run = "echo d", DependsOn = ["b", "c"] }
        );

        // Act
        var plan = _scheduler.BuildExecutionPlan(workflow);

        // Assert
        plan.Waves.Should().HaveCount(3);
        plan.Waves[0].Tasks.Single().Id.Should().Be("a");
        plan.Waves[1].Tasks.Should().HaveCount(2);
        plan.Waves[2].Tasks.Single().Id.Should().Be("d");
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
}
