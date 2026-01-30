using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Expressions;

namespace WorkflowEngine.Tests.Expressions;

public class StringFunctionsTests
{
    private readonly ExpressionEvaluator _evaluator = new();
    private readonly WorkflowContext _context;

    public StringFunctionsTests()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Environment = new Dictionary<string, string>
            {
                ["BRANCH"] = "feature/my-feature",
                ["FILE"] = "config.json",
                ["MESSAGE"] = "Build completed successfully"
            }
        };
        _context = new WorkflowContext
        {
            Workflow = workflow,
            Environment = workflow.Environment
        };
    }

    [Theory]
    [InlineData("contains(env.MESSAGE, 'completed')", true)]
    [InlineData("contains(env.MESSAGE, 'failed')", false)]
    [InlineData("contains(env.MESSAGE, 'COMPLETED')", true)] // Case insensitive
    public void Contains_EvaluatesCorrectly(string expression, bool expected)
    {
        var result = _evaluator.EvaluateCondition($"${{{{ {expression} }}}}", _context);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("startsWith(env.BRANCH, 'feature/')", true)]
    [InlineData("startsWith(env.BRANCH, 'hotfix/')", false)]
    [InlineData("startsWith(env.BRANCH, 'FEATURE/')", true)] // Case insensitive
    public void StartsWith_EvaluatesCorrectly(string expression, bool expected)
    {
        var result = _evaluator.EvaluateCondition($"${{{{ {expression} }}}}", _context);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("endsWith(env.FILE, '.json')", true)]
    [InlineData("endsWith(env.FILE, '.yaml')", false)]
    [InlineData("endsWith(env.FILE, '.JSON')", true)] // Case insensitive
    public void EndsWith_EvaluatesCorrectly(string expression, bool expected)
    {
        var result = _evaluator.EvaluateCondition($"${{{{ {expression} }}}}", _context);
        result.Should().Be(expected);
    }

    [Fact]
    public void Equals_EvaluatesCorrectly()
    {
        var result = _evaluator.EvaluateCondition("${{ equals(env.FILE, 'config.json') }}", _context);
        result.Should().BeTrue();

        result = _evaluator.EvaluateCondition("${{ equals(env.FILE, 'other.json') }}", _context);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_WithEmptyValue_ReturnsTrue()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Environment = new Dictionary<string, string> { ["EMPTY"] = "" }
        };
        var context = new WorkflowContext
        {
            Workflow = workflow,
            Environment = workflow.Environment
        };

        var result = _evaluator.EvaluateCondition("${{ isEmpty(env.EMPTY) }}", context);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WithNonEmptyValue_ReturnsFalse()
    {
        var result = _evaluator.EvaluateCondition("${{ isEmpty(env.BRANCH) }}", _context);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNotEmpty_WithNonEmptyValue_ReturnsTrue()
    {
        var result = _evaluator.EvaluateCondition("${{ isNotEmpty(env.BRANCH) }}", _context);
        result.Should().BeTrue();
    }

    [Fact]
    public void StringFunctions_WithLiteralStrings_Work()
    {
        var result = _evaluator.EvaluateCondition("${{ contains('hello world', 'world') }}", _context);
        result.Should().BeTrue();

        result = _evaluator.EvaluateCondition("${{ startsWith('hello world', 'hello') }}", _context);
        result.Should().BeTrue();

        result = _evaluator.EvaluateCondition("${{ endsWith('hello world', 'world') }}", _context);
        result.Should().BeTrue();
    }

    [Fact]
    public void StringFunctions_CombinedWithAnd_Work()
    {
        // First verify each part works individually
        var part1 = _evaluator.EvaluateCondition("${{ startsWith(env.BRANCH, 'feature/') }}", _context);
        var part2 = _evaluator.EvaluateCondition("${{ contains(env.MESSAGE, 'success') }}", _context);

        part1.Should().BeTrue("startsWith should match feature branch");
        part2.Should().BeTrue("contains should find 'success' in message");

        // Now test the combined expression
        var result = _evaluator.EvaluateCondition(
            "${{ startsWith(env.BRANCH, 'feature/') && contains(env.MESSAGE, 'success') }}",
            _context);
        result.Should().BeTrue();
    }

    [Fact]
    public void StringFunctions_CombinedWithOr_Work()
    {
        // Feature branch OR hotfix branch
        var result = _evaluator.EvaluateCondition(
            "${{ startsWith(env.BRANCH, 'feature/') || startsWith(env.BRANCH, 'hotfix/') }}",
            _context);
        result.Should().BeTrue();
    }

    [Fact]
    public void StringFunctions_WithNegation_Work()
    {
        // NOT a main branch
        var result = _evaluator.EvaluateCondition(
            "${{ !startsWith(env.BRANCH, 'main') }}",
            _context);
        result.Should().BeTrue();
    }

    [Fact]
    public void StringFunctions_WithTaskOutput_Work()
    {
        // Add a task result with output
        var taskResult = new TaskResult
        {
            TaskId = "build",
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            Output = new TaskOutput { StandardOutput = "Build output: SUCCESS" },
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow
        };
        _context.RecordTaskResult(taskResult);

        var result = _evaluator.EvaluateCondition(
            "${{ contains(tasks.build.output, 'SUCCESS') }}",
            _context);
        result.Should().BeTrue();

        result = _evaluator.EvaluateCondition(
            "${{ contains(tasks.build.output, 'FAILED') }}",
            _context);
        result.Should().BeFalse();
    }
}
