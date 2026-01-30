using FluentAssertions;
using NSubstitute;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution;
using WorkflowEngine.Execution.Docker;
using WorkflowEngine.Execution.Ssh;
using WorkflowEngine.Execution.Strategies;

namespace WorkflowEngine.Tests.Execution.Strategies;

public class LocalExecutionStrategyTests
{
    private readonly IShellProvider _shellProvider;
    private readonly LocalExecutionStrategy _strategy;

    public LocalExecutionStrategyTests()
    {
        _shellProvider = new DefaultShellProvider();
        _strategy = new LocalExecutionStrategy(_shellProvider);
    }

    [Fact]
    public void Priority_Returns100()
    {
        _strategy.Priority.Should().Be(100);
    }

    [Fact]
    public void Name_ReturnsLocal()
    {
        _strategy.Name.Should().Be("Local");
    }

    [Fact]
    public void CanHandle_AlwaysReturnsTrue()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };

        _strategy.CanHandle(workflow, task).Should().BeTrue();
    }

    [Fact]
    public void BuildConfig_ReturnsShellExecutable()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask { Id = "test", Run = "echo hello", Shell = "bash" };
        var context = new WorkflowContext { Workflow = workflow };

        var config = _strategy.BuildConfig("echo hello", task, context);

        config.Executable.Should().NotBeNullOrEmpty();
        config.Arguments.Should().Contain("-c");
    }

    [Fact]
    public void BuildConfig_SetsEnvironmentVariables()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo $VAR",
            Environment = new Dictionary<string, string> { ["VAR"] = "value" }
        };
        var context = new WorkflowContext { Workflow = workflow };

        var config = _strategy.BuildConfig("echo $VAR", task, context);

        var envDict = new Dictionary<string, string>();
        config.EnvironmentAction(envDict);
        envDict.Should().ContainKey("VAR").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void BuildConfig_UsesTaskWorkingDirectory()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask { Id = "test", Run = "pwd", WorkingDirectory = "/tmp" };
        var context = new WorkflowContext { Workflow = workflow };

        var config = _strategy.BuildConfig("pwd", task, context);

        config.WorkingDirectory.Should().Be("/tmp");
    }
}

public class DockerExecutionStrategyTests
{
    private readonly IDockerCommandBuilder _dockerBuilder;
    private readonly DockerExecutionStrategy _strategy;

    public DockerExecutionStrategyTests()
    {
        _dockerBuilder = new DockerCommandBuilder(new DefaultShellProvider());
        _strategy = new DockerExecutionStrategy(_dockerBuilder);
    }

    [Fact]
    public void Priority_Returns20()
    {
        _strategy.Priority.Should().Be(20);
    }

    [Fact]
    public void Name_ReturnsDocker()
    {
        _strategy.Name.Should().Be("Docker");
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenNoDockerConfig()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };

        _strategy.CanHandle(workflow, task).Should().BeFalse();
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenWorkflowHasDockerConfig()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Docker = new DockerConfig { Container = "test-container" }
        };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };

        _strategy.CanHandle(workflow, task).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenTaskHasDockerConfig()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Docker = new DockerConfig { Container = "test-container" }
        };

        _strategy.CanHandle(workflow, task).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenDockerDisabled()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Docker = new DockerConfig { Container = "test-container" }
        };
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Docker = new DockerConfig { Disabled = true }
        };

        _strategy.CanHandle(workflow, task).Should().BeFalse();
    }

    [Fact]
    public void BuildConfig_ReturnsDockerExecutable()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Docker = new DockerConfig { Container = "test-container" }
        };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };
        var context = new WorkflowContext { Workflow = workflow };

        var config = _strategy.BuildConfig("echo hello", task, context);

        config.Executable.Should().Be("docker");
        config.Arguments.Should().Contain("exec");
        config.Arguments.Should().Contain("test-container");
    }
}

public class SshExecutionStrategyTests
{
    private readonly ISshCommandBuilder _sshBuilder;
    private readonly SshExecutionStrategy _strategy;

    public SshExecutionStrategyTests()
    {
        _sshBuilder = new SshCommandBuilder(new DefaultShellProvider());
        _strategy = new SshExecutionStrategy(_sshBuilder);
    }

    [Fact]
    public void Priority_Returns10()
    {
        _strategy.Priority.Should().Be(10);
    }

    [Fact]
    public void Name_ReturnsSsh()
    {
        _strategy.Name.Should().Be("SSH");
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenNoSshConfig()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };

        _strategy.CanHandle(workflow, task).Should().BeFalse();
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenWorkflowHasSshConfig()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Ssh = new SshConfig { Host = "example.com", User = "user" }
        };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };

        _strategy.CanHandle(workflow, task).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ReturnsTrue_WhenTaskHasSshConfig()
    {
        var workflow = new Workflow { Name = "Test", Tasks = [] };
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Ssh = new SshConfig { Host = "example.com", User = "user" }
        };

        _strategy.CanHandle(workflow, task).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenSshDisabled()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Ssh = new SshConfig { Host = "example.com", User = "user" }
        };
        var task = new WorkflowTask
        {
            Id = "test",
            Run = "echo hello",
            Ssh = new SshConfig { Disabled = true }
        };

        _strategy.CanHandle(workflow, task).Should().BeFalse();
    }

    [Fact]
    public void BuildConfig_ReturnsSshExecutable()
    {
        var workflow = new Workflow
        {
            Name = "Test",
            Tasks = [],
            Ssh = new SshConfig { Host = "example.com", User = "user" }
        };
        var task = new WorkflowTask { Id = "test", Run = "echo hello" };
        var context = new WorkflowContext { Workflow = workflow };

        var config = _strategy.BuildConfig("echo hello", task, context);

        config.Executable.Should().Be("ssh");
        config.Arguments.Should().Contain("user@example.com");
    }
}

public class EnvironmentMergerTests
{
    [Fact]
    public void Merge_CombinesBasesAndOverrides()
    {
        var baseEnv = new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" };
        var overrideEnv = new Dictionary<string, string> { ["B"] = "3", ["C"] = "4" };

        var result = EnvironmentMerger.Merge(baseEnv, overrideEnv);

        result.Should().HaveCount(3);
        result["A"].Should().Be("1");
        result["B"].Should().Be("3"); // Override wins
        result["C"].Should().Be("4");
    }

    [Fact]
    public void Merge_HandlesEmptyDictionaries()
    {
        var baseEnv = new Dictionary<string, string> { ["A"] = "1" };
        var emptyEnv = new Dictionary<string, string>();

        var result = EnvironmentMerger.Merge(baseEnv, emptyEnv);

        result.Should().ContainKey("A").WhoseValue.Should().Be("1");
    }

    [Fact]
    public void MergeReadOnly_ReturnsBaseWhenOverrideEmpty()
    {
        var baseEnv = new Dictionary<string, string> { ["A"] = "1" };
        var emptyEnv = new Dictionary<string, string>();

        var result = EnvironmentMerger.MergeReadOnly(baseEnv, emptyEnv);

        result.Should().BeSameAs(baseEnv);
    }

    [Fact]
    public void MergeReadOnly_ReturnsOverrideWhenBaseEmpty()
    {
        var emptyEnv = new Dictionary<string, string>();
        var overrideEnv = new Dictionary<string, string> { ["A"] = "1" };

        var result = EnvironmentMerger.MergeReadOnly(emptyEnv, overrideEnv);

        result.Should().BeSameAs(overrideEnv);
    }

    [Fact]
    public void Merge_MultipleParams_CombinesAll()
    {
        var env1 = new Dictionary<string, string> { ["A"] = "1" };
        var env2 = new Dictionary<string, string> { ["B"] = "2" };
        var env3 = new Dictionary<string, string> { ["A"] = "3", ["C"] = "3" };

        var result = EnvironmentMerger.Merge(env1, env2, env3);

        result.Should().HaveCount(3);
        result["A"].Should().Be("3"); // Last wins
        result["B"].Should().Be("2");
        result["C"].Should().Be("3");
    }
}
