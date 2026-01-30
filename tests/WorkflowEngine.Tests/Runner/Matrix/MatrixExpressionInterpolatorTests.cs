using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Matrix;

namespace WorkflowEngine.Tests.Runner.Matrix;

public class MatrixExpressionInterpolatorTests
{
    private readonly MatrixExpressionInterpolator _interpolator = new();

    [Fact]
    public void Interpolate_WithMatrixExpression_ReplacesValue()
    {
        // Arrange
        var input = "echo ${{ matrix.os }}";
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _interpolator.Interpolate(input, values);

        // Assert
        result.Should().Be("echo ubuntu");
    }

    [Fact]
    public void Interpolate_WithMultipleExpressions_ReplacesAll()
    {
        // Arrange
        var input = "${{ matrix.os }}-${{ matrix.version }}";
        var values = new Dictionary<string, string>
        {
            ["os"] = "ubuntu",
            ["version"] = "22.04"
        };

        // Act
        var result = _interpolator.Interpolate(input, values);

        // Assert
        result.Should().Be("ubuntu-22.04");
    }

    [Fact]
    public void Interpolate_WithUnknownKey_KeepsOriginal()
    {
        // Arrange
        var input = "${{ matrix.unknown }}";
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _interpolator.Interpolate(input, values);

        // Assert
        result.Should().Be("${{ matrix.unknown }}");
    }

    [Fact]
    public void Interpolate_WithSpacesInExpression_StillWorks()
    {
        // Arrange
        var input = "${{   matrix.os   }}";
        var values = new Dictionary<string, string> { ["os"] = "ubuntu" };

        // Act
        var result = _interpolator.Interpolate(input, values);

        // Assert
        result.Should().Be("ubuntu");
    }

    [Fact]
    public void InterpolateEnvironment_ReplacesValuesInDictionary()
    {
        // Arrange
        var env = new Dictionary<string, string>
        {
            ["PLATFORM"] = "${{ matrix.os }}",
            ["VERSION"] = "${{ matrix.version }}"
        };
        var values = new Dictionary<string, string>
        {
            ["os"] = "ubuntu",
            ["version"] = "22.04"
        };

        // Act
        var result = _interpolator.InterpolateEnvironment(env, values);

        // Assert
        result["PLATFORM"].Should().Be("ubuntu");
        result["VERSION"].Should().Be("22.04");
    }

    [Fact]
    public void InterpolateInput_WithTextValue_ReplacesExpression()
    {
        // Arrange
        var input = new TaskInput
        {
            Type = InputType.Text,
            Value = "${{ matrix.data }}"
        };
        var values = new Dictionary<string, string> { ["data"] = "test-data" };

        // Act
        var result = _interpolator.InterpolateInput(input, values);

        // Assert
        result!.Value.Should().Be("test-data");
    }

    [Fact]
    public void InterpolateInput_WithFilePath_ReplacesExpression()
    {
        // Arrange
        var input = new TaskInput
        {
            Type = InputType.File,
            FilePath = "/data/${{ matrix.env }}/config.json"
        };
        var values = new Dictionary<string, string> { ["env"] = "prod" };

        // Act
        var result = _interpolator.InterpolateInput(input, values);

        // Assert
        result!.FilePath.Should().Be("/data/prod/config.json");
    }

    [Fact]
    public void InterpolateInput_WithNull_ReturnsNull()
    {
        // Arrange
        var values = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var result = _interpolator.InterpolateInput(null, values);

        // Assert
        result.Should().BeNull();
    }
}
