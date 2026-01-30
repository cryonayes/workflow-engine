using FluentAssertions;
using WorkflowEngine.Parsing.Mappers;

namespace WorkflowEngine.Tests.Parsing.Mappers;

public class MatrixConfigMapperTests
{
    [Fact]
    public void Map_WithNull_ReturnsNull()
    {
        var result = MatrixConfigMapper.Map(null);
        result.Should().BeNull();
    }

    [Fact]
    public void Map_WithEmptyDictionary_ReturnsNull()
    {
        var result = MatrixConfigMapper.Map(new Dictionary<string, object>());
        result.Should().BeNull();
    }

    [Fact]
    public void Map_WithSingleDimension_MapsDimensions()
    {
        var matrix = new Dictionary<string, object>
        {
            ["os"] = new List<object> { "ubuntu", "macos", "windows" }
        };

        var result = MatrixConfigMapper.Map(matrix);

        result.Should().NotBeNull();
        result!.Dimensions.Should().ContainKey("os");
        result.Dimensions["os"].Should().BeEquivalentTo(["ubuntu", "macos", "windows"]);
    }

    [Fact]
    public void Map_WithMultipleDimensions_MapsDimensions()
    {
        var matrix = new Dictionary<string, object>
        {
            ["os"] = new List<object> { "ubuntu", "windows" },
            ["version"] = new List<object> { "3.10", "3.11" }
        };

        var result = MatrixConfigMapper.Map(matrix);

        result.Should().NotBeNull();
        result!.Dimensions.Should().HaveCount(2);
        result.Dimensions["os"].Should().HaveCount(2);
        result.Dimensions["version"].Should().HaveCount(2);
    }

    [Fact]
    public void Map_WithInclude_MapsInclude()
    {
        var matrix = new Dictionary<string, object>
        {
            ["os"] = new List<object> { "ubuntu" },
            ["include"] = new List<object>
            {
                new Dictionary<object, object>
                {
                    ["os"] = "alpine",
                    ["experimental"] = "true"
                }
            }
        };

        var result = MatrixConfigMapper.Map(matrix);

        result.Should().NotBeNull();
        result!.Include.Should().HaveCount(1);
        result.Include[0]["os"].Should().Be("alpine");
        result.Include[0]["experimental"].Should().Be("true");
    }

    [Fact]
    public void Map_WithExclude_MapsExclude()
    {
        var matrix = new Dictionary<string, object>
        {
            ["os"] = new List<object> { "ubuntu", "windows" },
            ["version"] = new List<object> { "3.10", "3.11" },
            ["exclude"] = new List<object>
            {
                new Dictionary<object, object>
                {
                    ["os"] = "windows",
                    ["version"] = "3.10"
                }
            }
        };

        var result = MatrixConfigMapper.Map(matrix);

        result.Should().NotBeNull();
        result!.Exclude.Should().HaveCount(1);
        result.Exclude[0]["os"].Should().Be("windows");
        result.Exclude[0]["version"].Should().Be("3.10");
    }

    [Fact]
    public void Map_WithOnlyInclude_ReturnsConfig()
    {
        var matrix = new Dictionary<string, object>
        {
            ["include"] = new List<object>
            {
                new Dictionary<object, object> { ["custom"] = "value1" },
                new Dictionary<object, object> { ["custom"] = "value2" }
            }
        };

        var result = MatrixConfigMapper.Map(matrix);

        result.Should().NotBeNull();
        result!.Dimensions.Should().BeEmpty();
        result.Include.Should().HaveCount(2);
    }

    [Fact]
    public void Map_WithStringValue_TreatsAsSingleValue()
    {
        var matrix = new Dictionary<string, object>
        {
            ["os"] = "ubuntu"
        };

        var result = MatrixConfigMapper.Map(matrix);

        result.Should().NotBeNull();
        result!.Dimensions["os"].Should().BeEquivalentTo(["ubuntu"]);
    }
}
