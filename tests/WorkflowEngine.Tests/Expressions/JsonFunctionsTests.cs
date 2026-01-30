using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Expressions;

namespace WorkflowEngine.Tests.Expressions;

public class JsonFunctionsTests
{
    private readonly JsonFunctions _jsonFunctions = new();
    private readonly VariableInterpolator _interpolator = new();

    #region IsFromJsonExpression Tests

    [Theory]
    [InlineData("fromJson(tasks.api.output).id", true)]
    [InlineData("fromJson(tasks.api.output)", true)]
    [InlineData("  fromJson(value).prop", true)]
    [InlineData("FROMJSON(value).prop", true)]
    [InlineData("tasks.api.output", false)]
    [InlineData("env.VAR", false)]
    [InlineData("", false)]
    public void IsFromJsonExpression_DetectsCorrectly(string expression, bool expected)
    {
        _jsonFunctions.IsFromJsonExpression(expression).Should().Be(expected);
    }

    #endregion

    #region ParseFromJsonExpression Tests

    [Fact]
    public void ParseFromJsonExpression_SimpleProperty_ParsesCorrectly()
    {
        var result = _jsonFunctions.ParseFromJsonExpression("fromJson(tasks.api.output).id");

        result.Should().NotBeNull();
        result!.Value.innerExpression.Should().Be("tasks.api.output");
        result!.Value.propertyPath.Should().Be("id");
    }

    [Fact]
    public void ParseFromJsonExpression_NestedProperty_ParsesCorrectly()
    {
        var result = _jsonFunctions.ParseFromJsonExpression("fromJson(tasks.api.output).user.profile.name");

        result.Should().NotBeNull();
        result!.Value.innerExpression.Should().Be("tasks.api.output");
        result!.Value.propertyPath.Should().Be("user.profile.name");
    }

    [Fact]
    public void ParseFromJsonExpression_ArrayIndex_ParsesCorrectly()
    {
        var result = _jsonFunctions.ParseFromJsonExpression("fromJson(tasks.api.output).items[0]");

        result.Should().NotBeNull();
        result!.Value.innerExpression.Should().Be("tasks.api.output");
        result!.Value.propertyPath.Should().Be("items[0]");
    }

    [Fact]
    public void ParseFromJsonExpression_NoPropertyPath_ParsesCorrectly()
    {
        var result = _jsonFunctions.ParseFromJsonExpression("fromJson(tasks.api.output)");

        result.Should().NotBeNull();
        result!.Value.innerExpression.Should().Be("tasks.api.output");
        result!.Value.propertyPath.Should().BeEmpty();
    }

    [Fact]
    public void ParseFromJsonExpression_InvalidExpression_ReturnsNull()
    {
        var result = _jsonFunctions.ParseFromJsonExpression("tasks.api.output");

        result.Should().BeNull();
    }

    #endregion

    #region ExtractJsonProperty Tests

    [Fact]
    public void ExtractJsonProperty_SimpleProperty_ReturnsValue()
    {
        var json = """{"id": "123", "name": "test"}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "id");

        result.Should().Be("123");
    }

    [Fact]
    public void ExtractJsonProperty_NestedProperty_ReturnsValue()
    {
        var json = """{"user": {"profile": {"name": "John"}}}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "user.profile.name");

        result.Should().Be("John");
    }

    [Fact]
    public void ExtractJsonProperty_ArrayIndex_ReturnsValue()
    {
        var json = """{"items": ["first", "second", "third"]}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "items[1]");

        result.Should().Be("second");
    }

    [Fact]
    public void ExtractJsonProperty_ArrayIndexWithProperty_ReturnsValue()
    {
        var json = """{"items": [{"id": 1, "name": "first"}, {"id": 2, "name": "second"}]}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "items[1].name");

        result.Should().Be("second");
    }

    [Fact]
    public void ExtractJsonProperty_NumberValue_ReturnsString()
    {
        var json = """{"count": 42, "price": 19.99}""";

        _jsonFunctions.ExtractJsonProperty(json, "count").Should().Be("42");
        _jsonFunctions.ExtractJsonProperty(json, "price").Should().Be("19.99");
    }

    [Fact]
    public void ExtractJsonProperty_BooleanValue_ReturnsString()
    {
        var json = """{"active": true, "disabled": false}""";

        _jsonFunctions.ExtractJsonProperty(json, "active").Should().Be("true");
        _jsonFunctions.ExtractJsonProperty(json, "disabled").Should().Be("false");
    }

    [Fact]
    public void ExtractJsonProperty_NullValue_ReturnsEmpty()
    {
        var json = """{"value": null}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "value");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractJsonProperty_ObjectValue_ReturnsJsonString()
    {
        var json = """{"data": {"nested": "value"}}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "data");

        result.Should().Be("""{"nested": "value"}""");
    }

    [Fact]
    public void ExtractJsonProperty_ArrayValue_ReturnsJsonString()
    {
        var json = """{"items": [1, 2, 3]}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "items");

        result.Should().Be("[1, 2, 3]");
    }

    [Fact]
    public void ExtractJsonProperty_NonExistentProperty_ReturnsEmpty()
    {
        var json = """{"id": "123"}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractJsonProperty_InvalidJson_ReturnsEmpty()
    {
        var json = "not valid json";

        var result = _jsonFunctions.ExtractJsonProperty(json, "id");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractJsonProperty_EmptyJson_ReturnsEmpty()
    {
        var result = _jsonFunctions.ExtractJsonProperty("", "id");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractJsonProperty_EmptyPath_ReturnsOriginalJson()
    {
        var json = """{"id": "123"}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "");

        result.Should().Be(json);
    }

    [Fact]
    public void ExtractJsonProperty_ArrayOutOfBounds_ReturnsEmpty()
    {
        var json = """{"items": ["one", "two"]}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "items[10]");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractJsonProperty_DeepNesting_ReturnsValue()
    {
        var json = """{"a": {"b": {"c": {"d": {"e": "deep"}}}}}""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "a.b.c.d.e");

        result.Should().Be("deep");
    }

    [Fact]
    public void ExtractJsonProperty_RootArrayAccess_ReturnsValue()
    {
        var json = """[{"id": 1}, {"id": 2}, {"id": 3}]""";

        var result = _jsonFunctions.ExtractJsonProperty(json, "[1].id");

        result.Should().Be("2");
    }

    #endregion

    #region Integration with VariableInterpolator

    [Fact]
    public void Resolve_FromJsonWithTaskOutput_ExtractsProperty()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput
            {
                StandardOutput = """{"id": "user-123", "name": "John Doe"}"""
            }
        });

        // Act
        var result = _interpolator.Resolve("fromJson(tasks.api.output).id", context);

        // Assert
        result.Should().Be("user-123");
    }

    [Fact]
    public void Resolve_FromJsonWithNestedProperty_ExtractsValue()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput
            {
                StandardOutput = """{"user": {"profile": {"email": "john@example.com"}}}"""
            }
        });

        // Act
        var result = _interpolator.Resolve("fromJson(tasks.api.output).user.profile.email", context);

        // Assert
        result.Should().Be("john@example.com");
    }

    [Fact]
    public void Resolve_FromJsonWithArrayAccess_ExtractsValue()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput
            {
                StandardOutput = """{"users": [{"name": "Alice"}, {"name": "Bob"}]}"""
            }
        });

        // Act
        var result = _interpolator.Resolve("fromJson(tasks.api.output).users[1].name", context);

        // Assert
        result.Should().Be("Bob");
    }

    [Fact]
    public void Resolve_FromJsonWithNoPath_ReturnsFullJson()
    {
        // Arrange
        var context = CreateContext();
        var jsonOutput = """{"id": "123"}""";
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput { StandardOutput = jsonOutput }
        });

        // Act
        var result = _interpolator.Resolve("fromJson(tasks.api.output)", context);

        // Assert
        result.Should().Be(jsonOutput);
    }

    [Fact]
    public void Resolve_FromJsonWithInvalidJson_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput { StandardOutput = "not json" }
        });

        // Act
        var result = _interpolator.Resolve("fromJson(tasks.api.output).id", context);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_FromJsonWithMissingTask_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = _interpolator.Resolve("fromJson(tasks.nonexistent.output).id", context);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Integration with ExpressionEvaluator

    [Fact]
    public void Interpolate_WithFromJson_ReplacesExpression()
    {
        // Arrange
        var evaluator = new ExpressionEvaluator();
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput
            {
                StandardOutput = """{"version": "2.0.0"}"""
            }
        });

        // Act
        var result = evaluator.Interpolate("Version is ${{ fromJson(tasks.api.output).version }}", context);

        // Assert
        result.Should().Be("Version is 2.0.0");
    }

    [Fact]
    public void Interpolate_WithMultipleFromJson_ReplacesAll()
    {
        // Arrange
        var evaluator = new ExpressionEvaluator();
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "api",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput
            {
                StandardOutput = """{"name": "test", "version": "1.0.0"}"""
            }
        });

        // Act
        var result = evaluator.Interpolate(
            "App: ${{ fromJson(tasks.api.output).name }} v${{ fromJson(tasks.api.output).version }}",
            context);

        // Assert
        result.Should().Be("App: test v1.0.0");
    }

    #endregion

    private static WorkflowContext CreateContext()
    {
        return new WorkflowContext
        {
            Workflow = new Workflow { Name = "Test", Tasks = [] },
            CancellationToken = CancellationToken.None
        };
    }
}
