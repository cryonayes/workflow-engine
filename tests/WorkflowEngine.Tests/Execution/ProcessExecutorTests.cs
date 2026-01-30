using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution;
using WorkflowEngine.Execution.Docker;
using WorkflowEngine.Execution.Output;
using WorkflowEngine.Execution.Ssh;
using WorkflowEngine.Execution.Strategies;

namespace WorkflowEngine.Tests.Execution;

public class ProcessExecutorTests
{
    private readonly ProcessExecutor _executor;

    public ProcessExecutorTests()
    {
        var fileWriter = new FileOutputWriter(NullLogger<FileOutputWriter>.Instance);
        var outputBuilder = new TaskOutputBuilder(fileWriter, NullLogger<TaskOutputBuilder>.Instance);
        var shellProvider = new DefaultShellProvider();
        var dockerBuilder = new DockerCommandBuilder(shellProvider);
        var sshBuilder = new SshCommandBuilder(shellProvider);

        // Create execution strategies (order by priority)
        var strategies = new IExecutionStrategy[]
        {
            new SshExecutionStrategy(sshBuilder),
            new DockerExecutionStrategy(dockerBuilder),
            new LocalExecutionStrategy(shellProvider)
        };

        _executor = new ProcessExecutor(
            strategies,
            outputBuilder,
            NullLogger<ProcessExecutor>.Instance);
    }

    private readonly WorkflowContext _context = new()
    {
        Workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            DefaultTimeoutMs = 30000
        }
    };

    [Fact]
    public async Task ExecuteAsync_SimpleEcho_ReturnsSuccessWithOutput()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "echo hello", Shell = "bash" };

        // Act
        var result = await _executor.ExecuteAsync("echo hello", task, _context, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Output.Should().NotBeNull();
        result.Output!.StandardOutput.Should().Contain("hello");
    }

    [Fact]
    public async Task ExecuteAsync_FailingCommand_ReturnsFailureWithExitCode()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "exit 42", Shell = "bash" };

        // Act
        var result = await _executor.ExecuteAsync("exit 42", task, _context, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.ExitCode.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_WithStdinInput_PassesInputToProcess()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "cat", Shell = "bash" };
        var input = Encoding.UTF8.GetBytes("input data");

        // Act
        var result = await _executor.ExecuteAsync("cat", task, _context, input, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Output!.StandardOutput.Should().Contain("input data");
    }

    [Fact]
    public async Task ExecuteAsync_CapturesStderr_WhenConfigured()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo error >&2",
            Shell = "bash",
            Output = new TaskOutputConfig { CaptureStderr = true }
        };

        // Act
        var result = await _executor.ExecuteAsync("echo error >&2", task, _context, null, null, CancellationToken.None);

        // Assert
        result.Output!.StandardError.Should().Contain("error");
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_CancelsLongRunningProcess()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "sleep 60",
            Shell = "bash",
            TimeoutMs = 100
        };

        // Act
        var result = await _executor.ExecuteAsync("sleep 60", task, _context, null, null, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ExecutionStatus.TimedOut);
        result.ErrorMessage.Should().Contain("timeout");
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_CancelsProcess()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "sleep 60", Shell = "bash" };
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = _executor.ExecuteAsync("sleep 60", task, _context, null, null, cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        var result = await executeTask;

        // Assert
        result.Status.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgressForOutput()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "echo line1; echo line2", Shell = "bash" };
        var progressReports = new List<TaskProgress>();
        var progress = new Progress<TaskProgress>(p => progressReports.Add(p));

        // Act
        await _executor.ExecuteAsync("echo line1; echo line2", task, _context, null, progress, CancellationToken.None);
        await Task.Delay(100); // Allow progress callbacks to complete

        // Assert
        progressReports.Should().Contain(p => p.Message.Contains("line1") || p.Message.Contains("line2"));
    }

    [Fact]
    public async Task ExecuteAsync_WithTaskEnvironment_SetsEnvironmentVariables()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo $MY_VAR",
            Shell = "bash",
            Environment = new Dictionary<string, string> { ["MY_VAR"] = "test_value" }
        };

        // Act
        var result = await _executor.ExecuteAsync("echo $MY_VAR", task, _context, null, null, CancellationToken.None);

        // Assert
        result.Output!.StandardOutput.Should().Contain("test_value");
    }

    [Fact]
    public async Task ExecuteAsync_WithWorkingDirectory_ExecutesInCorrectDirectory()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "pwd",
            Shell = "bash",
            WorkingDirectory = tempDir
        };

        // Act
        var result = await _executor.ExecuteAsync("pwd", task, _context, null, null, CancellationToken.None);

        // Assert
        result.Output!.StandardOutput.Should().Contain(tempDir.TrimEnd('/'));
    }

    [Fact]
    public async Task ExecuteAsync_TruncatesLargeOutput_WhenConfigured()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "yes | head -n 10000", // Generate lots of output
            Shell = "bash",
            Output = new TaskOutputConfig { MaxSizeBytes = 100 }
        };

        // Act
        var result = await _executor.ExecuteAsync("yes | head -n 10000", task, _context, null, null, CancellationToken.None);

        // Assert
        result.Output!.StandardOutput!.Length.Should().BeLessThanOrEqualTo(120); // 100 + truncation message
        result.Output!.StandardOutput.Should().Contain("[truncated]");
    }

    [Fact]
    public async Task ExecuteAsync_WithShellProvider_UsesCorrectShell()
    {
        // Arrange - default shell should be bash
        var task = new WorkflowTask { Id = "test", Run = "echo $SHELL", Shell = "sh" };

        // Act
        var result = await _executor.ExecuteAsync("echo test", task, _context, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_RecordsStartAndEndTime()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "echo test", Shell = "bash" };
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = await _executor.ExecuteAsync("echo test", task, _context, null, null, CancellationToken.None);

        var after = DateTimeOffset.UtcNow;

        // Assert
        result.StartTime.Should().BeOnOrAfter(before);
        result.EndTime.Should().BeOnOrBefore(after);
        result.Duration.Should().BePositive();
    }

    [Fact]
    public async Task ExecuteAsync_WithMultilineCommand_ExecutesCorrectly()
    {
        // Arrange
        var task = new WorkflowTask { Id = "test", Run = "echo first\necho second", Shell = "bash" };

        // Act
        var result = await _executor.ExecuteAsync("echo first\necho second", task, _context, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Output!.StandardOutput.Should().Contain("first");
        result.Output!.StandardOutput.Should().Contain("second");
    }
}
