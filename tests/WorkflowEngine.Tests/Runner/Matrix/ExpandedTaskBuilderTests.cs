using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Matrix;

namespace WorkflowEngine.Tests.Runner.Matrix;

public class ExpandedTaskBuilderTests
{
    private readonly ExpandedTaskBuilder _builder;

    public ExpandedTaskBuilderTests()
    {
        var interpolator = new MatrixExpressionInterpolator();
        _builder = new ExpandedTaskBuilder(interpolator);
    }

    [Fact]
    public void Build_InterpolatesRunCommand()
    {
        // Arrange
        var template = new WorkflowTask
        {
            Id = "test",
            Run = "echo ${{ matrix.message }}"
        };
        var values = new Dictionary<string, string> { ["message"] = "hello" };

        // Act
        var result = _builder.Build(template, values);

        // Assert
        result.Run.Should().Be("echo hello");
    }

    [Fact]
    public void Build_GeneratesUniqueIdWhenNoMatrixExpression()
    {
        // Arrange
        var template = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello"
        };
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _builder.Build(template, values);

        // Assert
        result.Id.Should().Be("test-ubuntu");
    }

    [Fact]
    public void Build_UsesInterpolatedIdWhenMatrixExpression()
    {
        // Arrange
        var template = new WorkflowTask
        {
            Id = "test-${{ matrix.os }}",
            Run = "echo hello"
        };
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _builder.Build(template, values);

        // Assert
        result.Id.Should().Be("test-ubuntu");
    }

    [Fact]
    public void Build_ClearsMatrixConfig()
    {
        // Arrange
        var template = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Matrix = new MatrixConfig
            {
                Dimensions = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["os"] = ["ubuntu"]
                }
            }
        };
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _builder.Build(template, values);

        // Assert
        result.Matrix.Should().BeNull();
    }

    [Fact]
    public void Build_SetsMatrixValues()
    {
        // Arrange
        var template = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello"
        };
        var values = new Dictionary<string, string>
        {
            ["os"] = "ubuntu",
            ["version"] = "22.04"
        };

        // Act
        var result = _builder.Build(template, values);

        // Assert
        result.MatrixValues.Should().BeEquivalentTo(values);
    }

    [Fact]
    public void Build_PreservesDependsOn()
    {
        // Arrange
        var template = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            DependsOn = ["setup", "build"]
        };
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _builder.Build(template, values);

        // Assert
        result.DependsOn.Should().BeEquivalentTo(["setup", "build"]);
    }

    [Fact]
    public void SanitizeIdComponent_ReplacesSpecialCharacters()
    {
        // Act & Assert
        _builder.SanitizeIdComponent("3.10").Should().Be("3_10");
        _builder.SanitizeIdComponent("ubuntu-22.04").Should().Be("ubuntu_22_04");
        _builder.SanitizeIdComponent("node@18").Should().Be("node_18");
    }

    [Fact]
    public void SanitizeIdComponent_TrimsUnderscores()
    {
        // Act & Assert
        _builder.SanitizeIdComponent(".test.").Should().Be("test");
        _builder.SanitizeIdComponent("__test__").Should().Be("test");
    }

    [Fact]
    public void GenerateTaskId_CombinesBaseIdWithValues()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["os"] = "ubuntu",
            ["version"] = "22.04"
        };

        // Act
        var result = _builder.GenerateTaskId("build", values);

        // Assert
        result.Should().Be("build-ubuntu-22_04");
    }
}
