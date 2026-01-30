using FluentAssertions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Expressions;

namespace WorkflowEngine.Tests.Expressions;

public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    [Fact]
    public void EvaluateCondition_SuccessFunction_ReturnsTrue_WhenAllDependenciesSucceeded()
    {
        // Arrange
        var context = CreateContext();
        AddSuccessfulResult(context, "dep1");
        AddSuccessfulResult(context, "dep2");

        // Act
        var result = _evaluator.EvaluateCondition("${{ success() }}", context, ["dep1", "dep2"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_SuccessFunction_ReturnsFalse_WhenAnyDependencyFailed()
    {
        // Arrange
        var context = CreateContext();
        AddSuccessfulResult(context, "dep1");
        AddFailedResult(context, "dep2");

        // Act
        var result = _evaluator.EvaluateCondition("${{ success() }}", context, ["dep1", "dep2"]);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_FailureFunction_ReturnsTrue_WhenAnyDependencyFailed()
    {
        // Arrange
        var context = CreateContext();
        AddFailedResult(context, "dep1");

        // Act
        var result = _evaluator.EvaluateCondition("${{ failure() }}", context, ["dep1"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_AlwaysFunction_AlwaysReturnsTrue()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = _evaluator.EvaluateCondition("${{ always() }}", context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_ExitCodeComparison_EvaluatesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        AddResultWithExitCode(context, "build", 0);

        // Act
        var result = _evaluator.EvaluateCondition("${{ tasks.build.exitcode == 0 }}", context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_NotEqualComparison_EvaluatesCorrectly()
    {
        // Arrange
        var env = new Dictionary<string, string> { ["DEPLOY_ENV"] = "staging" };
        var context = CreateContext(env);

        // Act
        var result = _evaluator.EvaluateCondition("${{ env.DEPLOY_ENV != 'production' }}", context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_LogicalAnd_EvaluatesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        AddSuccessfulResult(context, "dep1");
        AddResultWithExitCode(context, "dep1", 0);

        // Act
        var result = _evaluator.EvaluateCondition("${{ success() && tasks.dep1.exitcode == 0 }}", context, ["dep1"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_LogicalOr_EvaluatesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        AddFailedResult(context, "dep1");

        // Act
        var result = _evaluator.EvaluateCondition("${{ success() || failure() }}", context, ["dep1"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Negation_EvaluatesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        AddFailedResult(context, "dep1");

        // Act
        var result = _evaluator.EvaluateCondition("${{ !success() }}", context, ["dep1"]);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Interpolate_EnvironmentVariable_ReplacesCorrectly()
    {
        // Arrange
        var env = new Dictionary<string, string> { ["BUILD_CONFIG"] = "Release" };
        var context = CreateContext(env);
        var input = "dotnet build --configuration ${{ env.BUILD_CONFIG }}";

        // Act
        var result = _evaluator.Interpolate(input, context);

        // Assert
        result.Should().Be("dotnet build --configuration Release");
    }

    [Fact]
    public void Interpolate_TaskOutput_ReplacesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "build",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput { StandardOutput = "1.0.0" }
        });
        var input = "Version: ${{ tasks.build.output }}";

        // Act
        var result = _evaluator.Interpolate(input, context);

        // Assert
        result.Should().Be("Version: 1.0.0");
    }

    [Fact]
    public void Interpolate_TaskExitCode_ReplacesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "test",
            Status = ExecutionStatus.Failed,
            ExitCode = 1
        });
        var input = "Exit code: ${{ tasks.test.exitcode }}";

        // Act
        var result = _evaluator.Interpolate(input, context);

        // Assert
        result.Should().Be("Exit code: 1");
    }

    [Fact]
    public void Interpolate_WorkflowName_ReplacesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var input = "Running: ${{ workflow.name }}";

        // Act
        var result = _evaluator.Interpolate(input, context);

        // Assert
        result.Should().Be("Running: Test Workflow");
    }

    [Fact]
    public void Interpolate_MultipleVariables_ReplacesAll()
    {
        // Arrange
        var env = new Dictionary<string, string> { ["ENV"] = "production" };
        var context = CreateContext(env);
        context.RecordTaskResult(new TaskResult
        {
            TaskId = "build",
            Status = ExecutionStatus.Succeeded,
            Output = new TaskOutput { StandardOutput = "v2.0.0" }
        });
        var input = "Deploying ${{ tasks.build.output }} to ${{ env.ENV }}";

        // Act
        var result = _evaluator.Interpolate(input, context);

        // Assert
        result.Should().Be("Deploying v2.0.0 to production");
    }

    [Fact]
    public void Interpolate_NoVariables_ReturnsOriginal()
    {
        // Arrange
        var context = CreateContext();
        var input = "Just plain text without variables";

        // Act
        var result = _evaluator.Interpolate(input, context);

        // Assert
        result.Should().Be("Just plain text without variables");
    }

    private static WorkflowContext CreateContext(Dictionary<string, string>? env = null)
    {
        return new WorkflowContext
        {
            Workflow = new Workflow
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                Tasks = []
            },
            Environment = env ?? new Dictionary<string, string>()
        };
    }

    private static void AddSuccessfulResult(WorkflowContext context, string taskId)
    {
        context.RecordTaskResult(new TaskResult
        {
            TaskId = taskId,
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0
        });
    }

    private static void AddFailedResult(WorkflowContext context, string taskId)
    {
        context.RecordTaskResult(new TaskResult
        {
            TaskId = taskId,
            Status = ExecutionStatus.Failed,
            ExitCode = 1
        });
    }

    private static void AddResultWithExitCode(WorkflowContext context, string taskId, int exitCode)
    {
        context.RecordTaskResult(new TaskResult
        {
            TaskId = taskId,
            Status = exitCode == 0 ? ExecutionStatus.Succeeded : ExecutionStatus.Failed,
            ExitCode = exitCode
        });
    }
}
