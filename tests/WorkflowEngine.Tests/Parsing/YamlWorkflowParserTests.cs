using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Parsing;

namespace WorkflowEngine.Tests.Parsing;

public class YamlWorkflowParserTests
{
    private readonly YamlWorkflowParser _parser = new(
        new WorkflowValidator(),
        NullLogger<YamlWorkflowParser>.Instance);

    [Fact]
    public void Parse_BasicWorkflow_ReturnsCorrectWorkflow()
    {
        // Arrange
        var yaml = """
            name: Simple Workflow
            description: A simple test workflow
            tasks:
              - id: hello
                run: echo "Hello World"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Name.Should().Be("Simple Workflow");
        workflow.Description.Should().Be("A simple test workflow");
        workflow.Tasks.Should().HaveCount(1);
        workflow.Tasks[0].Id.Should().Be("hello");
        workflow.Tasks[0].Run.Should().Be("echo \"Hello World\"");
    }

    [Fact]
    public void Parse_WorkflowWithEnvironment_ParsesEnvironmentVariables()
    {
        // Arrange
        var yaml = """
            name: Env Test
            environment:
              BUILD_CONFIG: Release
              NODE_ENV: production
            tasks:
              - id: build
                run: echo "Building"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Environment.Should().ContainKey("BUILD_CONFIG").WhoseValue.Should().Be("Release");
        workflow.Environment.Should().ContainKey("NODE_ENV").WhoseValue.Should().Be("production");
    }

    [Fact]
    public void Parse_TaskWithDependencies_ParsesDependsOn()
    {
        // Arrange
        var yaml = """
            name: Dependencies Test
            tasks:
              - id: first
                run: echo "First"
              - id: second
                run: echo "Second"
                dependsOn:
                  - first
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[1].DependsOn.Should().Contain("first");
    }

    [Fact]
    public void Parse_TaskWithCondition_ParsesIfClause()
    {
        // Arrange
        var yaml = """
            name: Condition Test
            tasks:
              - id: conditional
                run: echo "Conditional"
                if: ${{ success() }}
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].If.Should().Be("${{ success() }}");
    }

    [Fact]
    public void Parse_TaskWithInput_ParsesInputConfig()
    {
        // Arrange
        var yaml = """
            name: Input Test
            tasks:
              - id: producer
                run: echo "data"
              - id: consumer
                run: cat
                input:
                  type: pipe
                  value: ${{ tasks.producer.output }}
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[1].Input.Should().NotBeNull();
        workflow.Tasks[1].Input!.Type.Should().Be(InputType.Pipe);
        workflow.Tasks[1].Input!.Value.Should().Be("${{ tasks.producer.output }}");
    }

    [Fact]
    public void Parse_TaskWithOutput_ParsesOutputConfig()
    {
        // Arrange
        var yaml = """
            name: Output Test
            tasks:
              - id: capture
                run: echo "captured"
                output:
                  type: string
                  captureStderr: true
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].Output.Should().NotBeNull();
        workflow.Tasks[0].Output!.Type.Should().Be(OutputType.String);
        workflow.Tasks[0].Output!.CaptureStderr.Should().BeTrue();
    }

    [Fact]
    public void Parse_TaskWithRetry_ParsesRetrySettings()
    {
        // Arrange
        var yaml = """
            name: Retry Test
            tasks:
              - id: flaky
                run: echo "might fail"
                retryCount: 3
                retryDelayMs: 1000
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].RetryCount.Should().Be(3);
        workflow.Tasks[0].RetryDelayMs.Should().Be(1000);
    }

    [Fact]
    public void Parse_TaskWithShell_ParsesShellSetting()
    {
        // Arrange
        var yaml = """
            name: Shell Test
            tasks:
              - id: powershell-task
                shell: pwsh
                run: Write-Host "Hello"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].Shell.Should().Be("pwsh");
    }

    [Fact]
    public void Parse_TaskWithTimeout_ParsesTimeoutMs()
    {
        // Arrange
        var yaml = """
            name: Timeout Test
            tasks:
              - id: slow
                run: sleep 100
                timeoutMs: 5000
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].TimeoutMs.Should().Be(5000);
    }

    [Fact]
    public void Parse_TaskWithContinueOnError_ParsesFlag()
    {
        // Arrange
        var yaml = """
            name: Continue Test
            tasks:
              - id: optional
                run: exit 1
                continueOnError: true
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].ContinueOnError.Should().BeTrue();
    }

    [Fact]
    public void Parse_WorkflowWithDefaultTimeout_ParsesDefaultTimeoutMs()
    {
        // Arrange
        var yaml = """
            name: Default Timeout Test
            defaultTimeoutMs: 60000
            tasks:
              - id: task
                run: echo "test"
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.DefaultTimeoutMs.Should().Be(60000);
    }

    [Fact]
    public void Parse_EmptyYaml_ThrowsException()
    {
        // Arrange
        var yaml = "";

        // Act & Assert
        var act = () => _parser.Parse(yaml);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_InvalidYaml_ThrowsException()
    {
        // Arrange
        var yaml = "{ invalid yaml content }}}}}";

        // Act & Assert
        var act = () => _parser.Parse(yaml);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_TaskWithName_ParsesDisplayName()
    {
        // Arrange
        var yaml = """
            name: Name Test
            tasks:
              - id: build-app
                name: Build Application
                run: dotnet build
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].Id.Should().Be("build-app");
        workflow.Tasks[0].Name.Should().Be("Build Application");
    }

    [Fact]
    public void Parse_TaskWithTaskEnvironment_ParsesTaskEnvVars()
    {
        // Arrange
        var yaml = """
            name: Task Env Test
            tasks:
              - id: custom-env
                run: echo $MY_VAR
                environment:
                  MY_VAR: my-value
            """;

        // Act
        var workflow = _parser.Parse(yaml);

        // Assert
        workflow.Tasks[0].Environment.Should().ContainKey("MY_VAR").WhoseValue.Should().Be("my-value");
    }
}
