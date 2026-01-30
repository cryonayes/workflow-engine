using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner;
using WorkflowEngine.Runner.Matrix;

namespace WorkflowEngine.Tests.Runner;

public class MatrixExpanderTests
{
    private readonly MatrixExpander _expander;

    public MatrixExpanderTests()
    {
        var combinationGenerator = new MatrixCombinationGenerator();
        var interpolator = new MatrixExpressionInterpolator();
        var taskBuilder = new ExpandedTaskBuilder(interpolator);
        var dependencyRewriter = new DependencyRewriter(taskBuilder);
        _expander = new MatrixExpander(combinationGenerator, taskBuilder, dependencyRewriter);
    }

    [Fact]
    public void Expand_WithNoMatrix_ReturnsSingleTask()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello"
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("test");
    }

    [Fact]
    public void Expand_WithSingleDimension_ExpandsCorrectly()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "macos", "windows"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.Id).Should().BeEquivalentTo(
            ["test-ubuntu", "test-macos", "test-windows"]);
    }

    [Fact]
    public void Expand_WithMultipleDimensions_GeneratesCartesianProduct()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }} ${{ matrix.version }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "macos"],
                    ["version"] = ["3.10", "3.11"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(4);
        result.Select(t => t.Run).Should().Contain("echo ubuntu 3.10");
        result.Select(t => t.Run).Should().Contain("echo ubuntu 3.11");
        result.Select(t => t.Run).Should().Contain("echo macos 3.10");
        result.Select(t => t.Run).Should().Contain("echo macos 3.11");
    }

    [Fact]
    public void Expand_WithExclude_FiltersOutCombinations()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }} ${{ matrix.version }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "windows"],
                    ["version"] = ["3.10", "3.11"]
                },
                Exclude =
                [
                    new Dictionary<string, string> { ["os"] = "windows", ["version"] = "3.10" }
                ]
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.Run).Should().NotContain("echo windows 3.10");
    }

    [Fact]
    public void Expand_WithInclude_AddsExtraCombinations()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "macos"]
                },
                Include =
                [
                    new Dictionary<string, string> { ["os"] = "alpine" }
                ]
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.Run).Should().Contain("echo alpine");
    }

    [Fact]
    public void Expand_WithIncludeAddingExtraKeys_MergesCorrectly()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "macos"]
                },
                Include =
                [
                    new Dictionary<string, string> { ["os"] = "ubuntu", ["experimental"] = "true" }
                ]
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(2);
        var ubuntuTask = result.First(t => t.Run == "echo ubuntu");
        ubuntuTask.MatrixValues.Should().ContainKey("experimental");
        ubuntuTask.MatrixValues!["experimental"].Should().Be("true");
    }

    [Fact]
    public void Expand_InterpolatesMatrixExpressionsInTaskId()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test-${{ matrix.os }}",
            Run = "echo hello",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "macos"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Id).Should().BeEquivalentTo(["test-ubuntu", "test-macos"]);
    }

    [Fact]
    public void Expand_InterpolatesMatrixExpressionsInEnvironment()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Environment = new Dictionary<string, string>
            {
                ["PLATFORM"] = "${{ matrix.os }}"
            },
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu", "macos"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(2);
        result.First(t => t.Environment["PLATFORM"] == "ubuntu").Should().NotBeNull();
        result.First(t => t.Environment["PLATFORM"] == "macos").Should().NotBeNull();
    }

    [Fact]
    public void Expand_ClearsMatrixConfigOnExpandedTasks()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result[0].Matrix.Should().BeNull();
    }

    [Fact]
    public void Expand_SetsMatrixValuesOnExpandedTasks()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.os }}",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result[0].MatrixValues.Should().NotBeNull();
        result[0].MatrixValues!["os"].Should().Be("ubuntu");
    }

    [Fact]
    public void ExpandAll_RewritesDependenciesToExpandedTaskIds()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new()
            {
                Id = "build",
                Run = "echo build ${{ matrix.os }}",
                Matrix = new MatrixConfig
                {
                    Dimensions = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["os"] = ["ubuntu", "macos"]
                    }
                }
            },
            new()
            {
                Id = "deploy",
                Run = "echo deploy",
                DependsOn = ["build"]
            }
        };

        // Act
        var result = _expander.ExpandAll(tasks);

        // Assert
        result.Should().HaveCount(3); // 2 build tasks + 1 deploy task
        var deployTask = result.First(t => t.Id == "deploy");
        deployTask.DependsOn.Should().BeEquivalentTo(["build-ubuntu", "build-macos"]);
    }

    [Fact]
    public void ExpandAll_WithMatrixDependingOnMatrix_MatchesByMatrixValues()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new()
            {
                Id = "build",
                Run = "echo build",
                Matrix = new MatrixConfig
                {
                    Dimensions = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["os"] = ["ubuntu", "macos"]
                    }
                }
            },
            new()
            {
                Id = "test",
                Run = "echo test",
                DependsOn = ["build"],
                Matrix = new MatrixConfig
                {
                    Dimensions = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["os"] = ["ubuntu", "macos"]
                    }
                }
            }
        };

        // Act
        var result = _expander.ExpandAll(tasks);

        // Assert
        result.Should().HaveCount(4); // 2 build + 2 test
        var testUbuntu = result.First(t => t.Id == "test-ubuntu");
        testUbuntu.DependsOn.Should().Contain("build-ubuntu");
        testUbuntu.DependsOn.Should().NotContain("build-macos");
    }

    [Fact]
    public void ExpandAll_PreservesNonMatrixTaskDependencies()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new()
            {
                Id = "setup",
                Run = "echo setup"
            },
            new()
            {
                Id = "test",
                Run = "echo test",
                DependsOn = ["setup"],
                Matrix = new MatrixConfig
                {
                    Dimensions = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["browser"] = ["chrome", "firefox"]
                    }
                }
            }
        };

        // Act
        var result = _expander.ExpandAll(tasks);

        // Assert
        result.Should().HaveCount(3); // 1 setup + 2 test
        foreach (var testTask in result.Where(t => t.Id.StartsWith("test")))
        {
            testTask.DependsOn.Should().Contain("setup");
        }
    }

    [Fact]
    public void Expand_SanitizesSpecialCharactersInTaskId()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["version"] = ["3.10", "3.11-beta"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(2);
        // Special characters should be replaced with underscores
        result.Select(t => t.Id).Should().AllSatisfy(id =>
        {
            id.Should().MatchRegex(@"^[a-zA-Z0-9_-]+$");
        });
    }

    [Fact]
    public void Expand_WithEmptyDimensionsAndInclude_OnlyUsesInclude()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.custom }}",
            Matrix = new MatrixConfig
            {
                Include =
                [
                    new Dictionary<string, string> { ["custom"] = "value1" },
                    new Dictionary<string, string> { ["custom"] = "value2" }
                ]
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Run).Should().BeEquivalentTo(["echo value1", "echo value2"]);
    }

    [Fact]
    public void Expand_InterpolatesMatrixExpressionsInName()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Name = "Test on ${{ matrix.os }}",
            Run = "echo hello",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        result[0].Name.Should().Be("Test on ubuntu");
    }

    [Fact]
    public void Expand_InterpolatesMatrixExpressionsInCondition()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            If = "${{ matrix.os }} == 'ubuntu'",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu"]
                }
            }
        };

        // Act
        var result = _expander.Expand(task);

        // Assert
        // The matrix expression ${{ matrix.os }} gets replaced with "ubuntu"
        result[0].If.Should().Be("ubuntu == 'ubuntu'");
    }
}
