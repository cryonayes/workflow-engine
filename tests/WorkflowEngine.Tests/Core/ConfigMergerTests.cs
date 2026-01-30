using FluentAssertions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Tests.Core;

public class ConfigMergerTests
{
    [Fact]
    public void GetEffectiveConfig_WithBothNull_ReturnsNull()
    {
        var result = ConfigMerger.GetEffectiveConfig<DockerConfig>(null, null);
        result.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveConfig_WithOnlyWorkflowConfig_ReturnsWorkflowConfig()
    {
        var workflowConfig = new DockerConfig { Container = "test-container" };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, null);

        result.Should().NotBeNull();
        result!.Container.Should().Be("test-container");
    }

    [Fact]
    public void GetEffectiveConfig_WithOnlyTaskConfig_ReturnsTaskConfig()
    {
        var taskConfig = new DockerConfig { Container = "task-container" };

        var result = ConfigMerger.GetEffectiveConfig<DockerConfig>(null, taskConfig);

        result.Should().NotBeNull();
        result!.Container.Should().Be("task-container");
    }

    [Fact]
    public void GetEffectiveConfig_TaskOverridesWorkflow()
    {
        var workflowConfig = new DockerConfig { Container = "workflow-container", User = "root" };
        var taskConfig = new DockerConfig { Container = "task-container" };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, taskConfig);

        result.Should().NotBeNull();
        result!.Container.Should().Be("task-container");
        result.User.Should().Be("root"); // Inherited from workflow
    }

    [Fact]
    public void GetEffectiveConfig_TaskDisabled_ReturnsNull()
    {
        var workflowConfig = new DockerConfig { Container = "workflow-container" };
        var taskConfig = new DockerConfig { Disabled = true };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, taskConfig);

        result.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveConfig_InvalidConfigAfterMerge_ReturnsNull()
    {
        // Docker config without container is invalid
        var workflowConfig = new DockerConfig { User = "root" };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, null);

        result.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveConfig_SshConfig_Works()
    {
        var workflowConfig = new SshConfig { Host = "example.com", User = "user" };
        var taskConfig = new SshConfig { Port = 2222 };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, taskConfig);

        result.Should().NotBeNull();
        result!.Host.Should().Be("example.com");
        result.User.Should().Be("user");
        result.Port.Should().Be(2222);
    }

    [Fact]
    public void GetEffectiveConfig_SshDisabled_ReturnsNull()
    {
        var workflowConfig = new SshConfig { Host = "example.com", User = "user" };
        var taskConfig = new SshConfig { Disabled = true };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, taskConfig);

        result.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveConfig_SshInvalidAfterMerge_ReturnsNull()
    {
        // SSH config without user is invalid
        var workflowConfig = new SshConfig { Host = "example.com" };

        var result = ConfigMerger.GetEffectiveConfig(workflowConfig, null);

        result.Should().BeNull();
    }
}
