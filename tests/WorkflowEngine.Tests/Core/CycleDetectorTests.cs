using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Tests.Core;

public class CycleDetectorTests
{
    [Fact]
    public void TryDetectCycle_WithNoCycle_ReturnsFalse()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "a", Run = "echo a" },
            new() { Id = "b", Run = "echo b", DependsOn = ["a"] },
            new() { Id = "c", Run = "echo c", DependsOn = ["b"] }
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeFalse();
        cyclePath.Should().BeNull();
    }

    [Fact]
    public void TryDetectCycle_WithDirectCycle_ReturnsTrue()
    {
        // Arrange: A -> B -> A
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "a", Run = "echo a", DependsOn = ["b"] },
            new() { Id = "b", Run = "echo b", DependsOn = ["a"] }
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeTrue();
        cyclePath.Should().NotBeNullOrEmpty();
        cyclePath.Should().Contain("a");
        cyclePath.Should().Contain("b");
    }

    [Fact]
    public void TryDetectCycle_WithIndirectCycle_ReturnsTrue()
    {
        // Arrange: A -> B -> C -> A
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "a", Run = "echo a", DependsOn = ["c"] },
            new() { Id = "b", Run = "echo b", DependsOn = ["a"] },
            new() { Id = "c", Run = "echo c", DependsOn = ["b"] }
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeTrue();
        cyclePath.Should().Contain(" -> ");
    }

    [Fact]
    public void TryDetectCycle_WithSelfCycle_ReturnsTrue()
    {
        // Arrange: A -> A
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "a", Run = "echo a", DependsOn = ["a"] }
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeTrue();
    }

    [Fact]
    public void TryDetectCycle_WithEmptyList_ReturnsFalse()
    {
        // Arrange
        var tasks = new List<WorkflowTask>();

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeFalse();
        cyclePath.Should().BeNull();
    }

    [Fact]
    public void TryDetectCycle_WithDiamondDependency_ReturnsFalse()
    {
        // Arrange: Diamond pattern (not a cycle)
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "a", Run = "echo a" },
            new() { Id = "b", Run = "echo b", DependsOn = ["a"] },
            new() { Id = "c", Run = "echo c", DependsOn = ["a"] },
            new() { Id = "d", Run = "echo d", DependsOn = ["b", "c"] }
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeFalse();
        cyclePath.Should().BeNull();
    }

    [Fact]
    public void TryDetectCycle_WithMissingDependency_ReturnsFalse()
    {
        // Arrange: B depends on non-existent task
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "a", Run = "echo a" },
            new() { Id = "b", Run = "echo b", DependsOn = ["nonexistent"] }
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeFalse();
    }

    [Fact]
    public void TryDetectCycle_IsCaseInsensitive()
    {
        // Arrange: References with different cases
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "TaskA", Run = "echo a" },
            new() { Id = "TaskB", Run = "echo b", DependsOn = ["taska"] }, // lowercase reference
            new() { Id = "TaskC", Run = "echo c", DependsOn = ["TASKB"] }  // uppercase reference
        };

        // Act
        var hasCycle = CycleDetector.TryDetectCycle(tasks, out var cyclePath);

        // Assert
        hasCycle.Should().BeFalse();
    }
}
