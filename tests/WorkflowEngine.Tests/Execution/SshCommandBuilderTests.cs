using FluentAssertions;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Execution;
using WorkflowEngine.Execution.Ssh;

namespace WorkflowEngine.Tests.Execution;

public class SshCommandBuilderTests
{
    private readonly IShellProvider _shellProvider = new DefaultShellProvider();
    private readonly SshCommandBuilder _builder;

    public SshCommandBuilderTests()
    {
        _builder = new SshCommandBuilder(_shellProvider);
    }

    #region BuildCommand Tests

    [Fact]
    public void BuildCommand_BasicConfig_ReturnsSshCommand()
    {
        var config = new SshConfig { Host = "example.com", User = "deploy" };
        var env = new Dictionary<string, string>();

        var (executable, args) = _builder.BuildCommand(config, "echo hello", env);

        executable.Should().Be("ssh");
        args.Should().Contain("deploy@example.com");
        args.Should().Contain("-o");
        args.Should().Contain("BatchMode=yes");
    }

    [Fact]
    public void BuildCommand_WithPort_AddsPortFlag()
    {
        var config = new SshConfig { Host = "example.com", User = "deploy", Port = 2222 };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().ContainInOrder("-p", "2222");
    }

    [Fact]
    public void BuildCommand_DefaultPort_OmitsPortFlag()
    {
        var config = new SshConfig { Host = "example.com", User = "deploy", Port = 22 };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().NotContain("-p");
    }

    [Fact]
    public void BuildCommand_WithPrivateKey_AddsIdentityFlag()
    {
        var config = new SshConfig
        {
            Host = "example.com",
            User = "deploy",
            PrivateKeyPath = "/home/user/.ssh/deploy_key"
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().ContainInOrder("-i", "/home/user/.ssh/deploy_key");
    }

    [Fact]
    public void BuildCommand_StrictHostKeyCheckingEnabled_AddsStrictOption()
    {
        var config = new SshConfig
        {
            Host = "example.com",
            User = "deploy",
            StrictHostKeyChecking = true
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("StrictHostKeyChecking=yes");
    }

    [Fact]
    public void BuildCommand_StrictHostKeyCheckingDisabled_AddsNoStrictOption()
    {
        var config = new SshConfig
        {
            Host = "example.com",
            User = "deploy",
            StrictHostKeyChecking = false
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("StrictHostKeyChecking=no");
    }

    [Fact]
    public void BuildCommand_WithConnectionTimeout_AddsTimeoutOption()
    {
        var config = new SshConfig
        {
            Host = "example.com",
            User = "deploy",
            ConnectionTimeoutSeconds = 60
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("ConnectTimeout=60");
    }

    [Fact]
    public void BuildCommand_WithExtraArgs_AddsExtraArgs()
    {
        var config = new SshConfig
        {
            Host = "example.com",
            User = "deploy",
            ExtraArgs = ["-v", "-A"]
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        args.Should().Contain("-v");
        args.Should().Contain("-A");
    }

    [Fact]
    public void BuildCommand_InvalidConfig_ThrowsArgumentException()
    {
        var config = new SshConfig { Host = null, User = null };

        var act = () => _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        act.Should().Throw<ArgumentException>().WithMessage("*valid host and user*");
    }

    [Fact]
    public void BuildCommand_MissingUser_ThrowsArgumentException()
    {
        var config = new SshConfig { Host = "example.com", User = null };

        var act = () => _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildCommand_WithEnvironment_IncludesExportInRemoteCommand()
    {
        var config = new SshConfig { Host = "example.com", User = "deploy" };
        var env = new Dictionary<string, string> { ["MY_VAR"] = "value" };

        var (_, args) = _builder.BuildCommand(config, "cmd", env);

        // The last argument should be the remote command
        var remoteCommand = args[^1];
        remoteCommand.Should().Contain("export MY_VAR=");
    }

    [Fact]
    public void BuildCommand_WithWorkingDirectory_IncludesCdInRemoteCommand()
    {
        var config = new SshConfig
        {
            Host = "example.com",
            User = "deploy",
            WorkingDirectory = "/app"
        };

        var (_, args) = _builder.BuildCommand(config, "cmd", new Dictionary<string, string>());

        var remoteCommand = args[^1];
        remoteCommand.Should().Contain("cd '/app'");
    }

    #endregion

    #region GetEffectiveConfig Tests

    [Fact]
    public void GetEffectiveConfig_NoSshConfig_ReturnsNull()
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
            Ssh = new SshConfig { Host = "example.com", User = "admin" }
        };
        var task = new WorkflowTask { Id = "task1", Run = "cmd" };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Host.Should().Be("example.com");
        result.User.Should().Be("admin");
    }

    [Fact]
    public void GetEffectiveConfig_TaskFullOverride_ReturnsTaskConfig()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Ssh = new SshConfig { Host = "example.com", User = "admin" }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Ssh = new SshConfig { Host = "db.example.com", User = "dbadmin" }
        };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Host.Should().Be("db.example.com");
        result.User.Should().Be("dbadmin");
    }

    [Fact]
    public void GetEffectiveConfig_TaskPartialOverride_MergesConfigs()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Ssh = new SshConfig
            {
                Host = "example.com",
                User = "admin",
                PrivateKeyPath = "~/.ssh/deploy_key"
            }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Ssh = new SshConfig { WorkingDirectory = "/tmp" } // Only override workingDir
        };

        var result = _builder.GetEffectiveConfig(workflow, task);

        result.Should().NotBeNull();
        result!.Host.Should().Be("example.com"); // Inherited
        result.User.Should().Be("admin"); // Inherited
        result.PrivateKeyPath.Should().Be("~/.ssh/deploy_key"); // Inherited
        result.WorkingDirectory.Should().Be("/tmp"); // Overridden
    }

    [Fact]
    public void GetEffectiveConfig_TaskDisablesSsh_ReturnsNull()
    {
        var workflow = new Workflow
        {
            Name = "test",
            Tasks = [],
            Ssh = new SshConfig { Host = "example.com", User = "admin" }
        };
        var task = new WorkflowTask
        {
            Id = "task1",
            Run = "cmd",
            Ssh = new SshConfig { Disabled = true }
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
            Ssh = new SshConfig
            {
                Host = "example.com",
                User = "admin",
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
            Ssh = new SshConfig
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

    #region SshConfig Tests

    [Fact]
    public void SshConfig_IsDisabled_ReturnsTrueForMissingHost()
    {
        new SshConfig { Host = "", User = "user" }.IsDisabled.Should().BeTrue();
        new SshConfig { Host = "   ", User = "user" }.IsDisabled.Should().BeTrue();
        new SshConfig { Host = null, User = "user" }.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void SshConfig_IsDisabled_ReturnsTrueWhenExplicitlyDisabled()
    {
        new SshConfig { Host = "valid", User = "user", Disabled = true }.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void SshConfig_IsValid_ReturnsTrueForValidConfig()
    {
        new SshConfig { Host = "example.com", User = "deploy" }.IsValid.Should().BeTrue();
        new SshConfig { Host = "example.com", User = "deploy", Port = 2222 }.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SshConfig_IsValid_ReturnsFalseForInvalidConfig()
    {
        new SshConfig { Host = null, User = "user" }.IsValid.Should().BeFalse();
        new SshConfig { Host = "", User = "user" }.IsValid.Should().BeFalse();
        new SshConfig { Host = "example.com", User = null }.IsValid.Should().BeFalse();
        new SshConfig { Host = "example.com", User = "" }.IsValid.Should().BeFalse();
        new SshConfig { Host = "valid", User = "user", Disabled = true }.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SshConfig_MergeWith_NullBase_ReturnsSelf()
    {
        var config = new SshConfig { Host = "example.com", User = "deploy" };

        var result = config.MergeWith(null);

        result.Host.Should().Be("example.com");
        result.User.Should().Be("deploy");
    }

    [Fact]
    public void SshConfig_MergeWith_InheritsUnsetProperties()
    {
        var baseConfig = new SshConfig
        {
            Host = "base-host.com",
            User = "base-user",
            PrivateKeyPath = "/base/key"
        };
        var overrideConfig = new SshConfig { User = "override-user" };

        var result = overrideConfig.MergeWith(baseConfig);

        result.Host.Should().Be("base-host.com"); // Inherited
        result.User.Should().Be("override-user"); // Overridden
        result.PrivateKeyPath.Should().Be("/base/key"); // Inherited
    }

    [Fact]
    public void SshConfig_ToString_ReturnsDescriptiveString()
    {
        new SshConfig { Host = "example.com", User = "deploy" }.ToString().Should().Be("SSH[deploy@example.com:22]");
        new SshConfig { Disabled = true }.ToString().Should().Be("SSH[disabled]");
        new SshConfig { Host = null, User = "user" }.ToString().Should().Be("SSH[partial]");
    }

    #endregion
}
