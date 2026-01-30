using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Matrix;

namespace WorkflowEngine.Tests.Runner.Matrix;

public class DependencyRewriterTests
{
    private readonly DependencyRewriter _rewriter;

    public DependencyRewriterTests()
    {
        var interpolator = new MatrixExpressionInterpolator();
        var taskBuilder = new ExpandedTaskBuilder(interpolator);
        _rewriter = new DependencyRewriter(taskBuilder);
    }

    [Fact]
    public void Rewrite_WithNoDependencies_ReturnsOriginalTask()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello"
        };
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>();

        // Act
        var result = _rewriter.Rewrite(task, expansionMap);

        // Assert
        result.Should().BeSameAs(task);
    }

    [Fact]
    public void Rewrite_WithExpandedDependency_RewritesToAllExpanded()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "deploy",
            Run = "echo deploy",
            DependsOn = ["build"]
        };
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>
        {
            ["build"] = ["build-ubuntu", "build-macos"]
        };

        // Act
        var result = _rewriter.Rewrite(task, expansionMap);

        // Assert
        result.DependsOn.Should().BeEquivalentTo(["build-ubuntu", "build-macos"]);
    }

    [Fact]
    public void Rewrite_WithMatchingMatrixValues_RewritesToMatchingOnly()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test-ubuntu",
            Run = "echo test",
            DependsOn = ["build"],
            MatrixValues = new Dictionary<string, string> { ["os"] = "ubuntu" }
        };
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>
        {
            ["build"] = ["build-ubuntu", "build-macos"]
        };

        // Act
        var result = _rewriter.Rewrite(task, expansionMap);

        // Assert
        result.DependsOn.Should().BeEquivalentTo(["build-ubuntu"]);
    }

    [Fact]
    public void Rewrite_WithNonExpandedDependency_KeepsOriginal()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo test",
            DependsOn = ["setup"]
        };
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>
        {
            ["build"] = ["build-ubuntu", "build-macos"]
        };

        // Act
        var result = _rewriter.Rewrite(task, expansionMap);

        // Assert
        result.DependsOn.Should().BeEquivalentTo(["setup"]);
    }

    [Fact]
    public void Rewrite_WithMixedDependencies_HandlesCorrectly()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "deploy",
            Run = "echo deploy",
            DependsOn = ["setup", "build"]
        };
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>
        {
            ["build"] = ["build-ubuntu", "build-macos"]
        };

        // Act
        var result = _rewriter.Rewrite(task, expansionMap);

        // Assert
        result.DependsOn.Should().BeEquivalentTo(["setup", "build-ubuntu", "build-macos"]);
    }

    [Fact]
    public void Rewrite_WhenNoChangesNeeded_ReturnsOriginalTask()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo test",
            DependsOn = ["setup"]
        };
        var expansionMap = new Dictionary<string, IReadOnlyList<string>>
        {
            ["setup"] = ["setup"] // Maps to same ID
        };

        // Act
        var result = _rewriter.Rewrite(task, expansionMap);

        // Assert
        result.Should().BeSameAs(task);
    }
}
