using FluentAssertions;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution;
using WorkflowEngine.Execution.Docker;

namespace WorkflowEngine.Tests.Execution;

public class DockerCommandBuilderTests
{
    private readonly IShellProvider _shellProvider = new DefaultShellProvider();
    private readonly DockerCommandBuilder _builder;

    public DockerCommandBuilderTests()
    {
        _builder = new DockerCommandBuilder(_shellProvider);
    }

    #region BuildCommand Tests

    [Fact]
    public void BuildCommand_BasicConfig_ReturnsDockerExecCommand()
    {
        var config = new DockerConfig { Container = "my-container" };
        var env = new Dictionary<string, string>();

        var (executable, args) = _builder.BuildCommand(config, "echo hello", env);

        executable.Should().Be("docker");
        args.Should().Contain("exec");
        args.Should().Contain("-i"); // Interactive by default
        args.Should().Contain("my-container");
        // Default shell is platform-dependent (bash on Unix, cmd on Windows)
        args.Should().EndWith([_shellProvider.DefaultShellType, "-c", "echo hello"]);
    }

    [Fact]
    public void BuildCommand_WithUser_AddsUserFlag()
    {
        var config = new DockerConfig { Container = "test", User = "hunter" };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().ContainInOrder("-u", "hunter");
    }

    [Fact]
    public void BuildCommand_WithWorkingDirectory_AddsWorkDirFlag()
    {
        var config = new DockerConfig { Container = "test", WorkingDirectory = "/app" };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().ContainInOrder("-w", "/app");
    }

    [Fact]
    public void BuildCommand_WithTty_AddsTtyFlag()
    {
        var config = new DockerConfig { Container = "test", Tty = true };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("-t");
    }

    [Fact]
    public void BuildCommand_WithPrivileged_AddsPrivilegedFlag()
    {
        var config = new DockerConfig { Container = "test", Privileged = true };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("--privileged");
    }

    [Fact]
    public void BuildCommand_WithEnvironment_AddsEnvFlags()
    {
        var config = new DockerConfig
        {
            Container = "test",
            Environment = new Dictionary<string, string> { ["MY_VAR"] = "value" }
        };
        var taskEnv = new Dictionary<string, string> { ["TASK_VAR"] = "task_value" };

        var (_, args) = _builder.BuildCommand(config, "cmd", taskEnv);

        args.Should().ContainInOrder("-e", "MY_VAR=value");
        args.Should().ContainInOrder("-e", "TASK_VAR=task_value");
    }

    [Fact]
    public void BuildCommand_WithExtraArgs_AddsExtraArgs()
    {
        var config = new DockerConfig
        {
            Container = "test",
            ExtraArgs = ["--cap-add=NET_ADMIN", "--memory=512m"]
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("--cap-add=NET_ADMIN");
        args.Should().Contain("--memory=512m");
    }

    [Fact]
    public void BuildCommand_InteractiveFalse_OmitsInteractiveFlag()
    {
        var config = new DockerConfig { Container = "test", Interactive = false };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().NotContain("-i");
    }

    [Fact]
    public void BuildCommand_InvalidConfig_ThrowsArgumentException()
    {
        var config = new DockerConfig { Container = null };

        var act = () => _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        act.Should().Throw<ArgumentException>().WithMessage("*valid container*");
    }

    #endregion

    #region GetEffectiveConfig Tests

    [Fact]
    public void GetEffectiveConfig_NoDockerConfig_ReturnsNull()
    {
        var workflow = new Workflow { Name = "test", Tasks = [] };
        var task = new WorkflowTask { Id = "task1", Run = "cmd" };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveConfig_OnlyWorkflowConfig_ReturnsWorkflowConfig()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Docker = new DockerConfig { Container = "workflow-container", User = "admin" }
        };
        var task = new WorkflowTask { Id = "task1", Run = "cmd" };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Container.Should().Be("workflow-container");
        result.User.Should().Be("admin");
    }

    [Fact]
    public void GetEffectiveConfig_TaskFullOverride_ReturnsTaskConfig()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Docker = new DockerConfig { Container = "workflow-container", User = "admin" }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Docker = new DockerConfig { Container = "task-container", User = "hunter" }
        };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Container.Should().Be("task-container");
        result.User.Should().Be("hunter");
    }

    [Fact]
    public void GetEffectiveConfig_TaskPartialOverride_MergesConfigs()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Docker = new DockerConfig
            {
                Container = "workflow-container",
                User = "admin",
                WorkingDirectory = "/app"
            }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Docker = new DockerConfig { WorkingDirectory = "/tmp" } // Only override workingDir
        };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Container.Should().Be("workflow-container"); // Inherited
        result.User.Should().Be("admin"); // Inherited
        result.WorkingDirectory.Should().Be("/tmp"); // Overridden
    }

    [Fact]
    public void GetEffectiveConfig_TaskDisablesDocker_ReturnsNull()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Docker = new DockerConfig { Container = "workflow-container" }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Docker = new DockerConfig { Disabled = true }
        };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveConfig_MergesEnvironmentVariables()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Docker = new DockerConfig
            {
                Container = "container",
                Environment = new Dictionary<string, string>
                {
                    ["WORKFLOW_VAR"] = "workflow_value",
                    ["SHARED_VAR"] = "workflow_shared"
                }
            }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Docker = new DockerConfig
            {
                Environment = new Dictionary<string, string>
                {
                    ["TASK_VAR"] = "task_value",
                    ["SHARED_VAR"] = "task_shared" // Override
                }
            }
        };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Environment.Should().ContainKey("WORKFLOW_VAR").WhoseValue.Should().Be("workflow_value");
        result.Environment.Should().ContainKey("TASK_VAR").WhoseValue.Should().Be("task_value");
        result.Environment.Should().ContainKey("SHARED_VAR").WhoseValue.Should().Be("task_shared");
    }

    #endregion

    #region DockerConfig Tests

    [Fact]
    public void DockerConfig_IsDisabled_ReturnsTrueForEmptyContainer()
    {
        new DockerConfig { Container = "" }.IsDisabled.Should().BeTrue();
        new DockerConfig { Container = "   " }.IsDisabled.Should().BeTrue();
        new DockerConfig { Container = null }.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void DockerConfig_IsDisabled_ReturnsTrueWhenExplicitlyDisabled()
    {
        new DockerConfig { Container = "valid", Disabled = true }.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void DockerConfig_IsValid_ReturnsTrueForValidConfig()
    {
        new DockerConfig { Container = "valid" }.IsValid.Should().BeTrue();
        new DockerConfig { Container = "valid", User = "user" }.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DockerConfig_IsValid_ReturnsFalseForInvalidConfig()
    {
        new DockerConfig { Container = null }.IsValid.Should().BeFalse();
        new DockerConfig { Container = "" }.IsValid.Should().BeFalse();
        new DockerConfig { Container = "valid", Disabled = true }.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DockerConfig_MergeWith_NullBase_ReturnsSelf()
    {
        var config = new DockerConfig { Container = "my-container", User = "user" };

        var result = config.MergeWith(null);

        result.Container.Should().Be("my-container");
        result.User.Should().Be("user");
    }

    [Fact]
    public void DockerConfig_MergeWith_InheritsUnsetProperties()
    {
        var baseConfig = new DockerConfig
        {
            Container = "base-container",
            User = "base-user",
            WorkingDirectory = "/base"
        };
        var overrideConfig = new DockerConfig { User = "override-user" };

        var result = overrideConfig.MergeWith(baseConfig);

        result.Container.Should().Be("base-container"); // Inherited
        result.User.Should().Be("override-user"); // Overridden
        result.WorkingDirectory.Should().Be("/base"); // Inherited
    }

    [Fact]
    public void DockerConfig_ToString_ReturnsDescriptiveString()
    {
        new DockerConfig { Container = "my-app" }.ToString().Should().Be("Docker[my-app]");
        new DockerConfig { Disabled = true }.ToString().Should().Be("Docker[disabled]");
        new DockerConfig { Container = null, User = "user" }.ToString().Should().Be("Docker[partial]");
    }

    #endregion
}
