using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Runner.Matrix;

namespace WorkflowEngine.Tests.Runner.Matrix;

public class MatrixCombinationGeneratorTests
{
    private readonly MatrixCombinationGenerator _generator = new();

    [Fact]
    public void Generate_WithSingleDimension_ReturnsAllValues()
    {
        // Arrange
        var matrix = new MatrixConfig
        {
            Dimensions = new Dictionary<string, IReadOnlyList<string>>
            {
                ["os"] = ["ubuntu", "macos", "windows"]
            }
        };

        // Act
        var result = _generator.Generate(matrix);

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c["os"]).Should().BeEquivalentTo(["ubuntu", "macos", "windows"]);
    }

    [Fact]
    public void Generate_WithMultipleDimensions_ReturnsCartesianProduct()
    {
        // Arrange
        var matrix = new MatrixConfig
        {
            Dimensions = new Dictionary<string, IReadOnlyList<string>>
            {
                ["os"] = ["ubuntu", "windows"],
                ["version"] = ["3.10", "3.11"]
            }
        };

        // Act
        var result = _generator.Generate(matrix);

        // Assert
        result.Should().HaveCount(4);
    }

    [Fact]
    public void Generate_WithExclusions_FiltersCombinations()
    {
        // Arrange
        var matrix = new MatrixConfig
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
        };

        // Act
        var result = _generator.Generate(matrix);

        // Assert
        result.Should().HaveCount(3);
        result.Should().NotContain(c => c["os"] == "windows" && c["version"] == "3.10");
    }

    [Fact]
    public void Generate_WithInclusions_AddsCombinations()
    {
        // Arrange
        var matrix = new MatrixConfig
        {
            Dimensions = new Dictionary<string, IReadOnlyList<string>>
            {
                ["os"] = ["ubuntu"]
            },
            Include =
            [
                new Dictionary<string, string> { ["os"] = "alpine" }
            ]
        };

        // Act
        var result = _generator.Generate(matrix);

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c["os"]).Should().Contain("alpine");
    }

    [Fact]
    public void Generate_WithInclusionMerge_AddsExtraKeys()
    {
        // Arrange
        var matrix = new MatrixConfig
        {
            Dimensions = new Dictionary<string, IReadOnlyList<string>>
            {
                ["os"] = ["ubuntu", "macos"]
            },
            Include =
            [
                new Dictionary<string, string> { ["os"] = "ubuntu", ["extra"] = "value" }
            ]
        };

        // Act
        var result = _generator.Generate(matrix);

        // Assert
        result.Should().HaveCount(2);
        var ubuntu = result.First(c => c["os"] == "ubuntu");
        ubuntu.Should().ContainKey("extra");
        ubuntu["extra"].Should().Be("value");
    }

    [Fact]
    public void Generate_WithEmptyDimensionsAndInclusions_ReturnsInclusions()
    {
        // Arrange
        var matrix = new MatrixConfig
        {
            Include =
            [
                new Dictionary<string, string> { ["custom"] = "value1" },
                new Dictionary<string, string> { ["custom"] = "value2" }
            ]
        };

        // Act
        var result = _generator.Generate(matrix);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Generate_WithNullMatrix_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _generator.Generate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
