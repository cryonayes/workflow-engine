using FluentAssertions;
using WorkflowEngine.Parsing.Dtos;
using WorkflowEngine.Parsing.Mappers;

namespace WorkflowEngine.Tests.Parsing.Mappers;

public class ExecutionConfigMapperTests
{
    [Fact]
    public void MapDocker_WithNull_ReturnsNull()
    {
        var result = ExecutionConfigMapper.MapDocker(null);
        result.Should().BeNull();
    }

    [Fact]
    public void MapDocker_WithEmptyDto_ReturnsNull()
    {
        var dto = new DockerDto();
        var result = ExecutionConfigMapper.MapDocker(dto);
        result.Should().BeNull();
    }

    [Fact]
    public void MapDocker_WithContainer_ReturnsConfig()
    {
        var dto = new DockerDto { Container = "my-container" };

        var result = ExecutionConfigMapper.MapDocker(dto);

        result.Should().NotBeNull();
        result!.Container.Should().Be("my-container");
    }

    [Fact]
    public void MapDocker_WithAllProperties_MapsCorrectly()
    {
        var dto = new DockerDto
        {
            Container = "my-container",
            User = "root",
            WorkingDirectory = "/app",
            Environment = new Dictionary<string, string> { ["KEY"] = "value" },
            Interactive = true,
            Tty = true,
            Privileged = false,
            Host = "docker.local",
            ExtraArgs = ["--rm"],
            Disabled = false
        };

        var result = ExecutionConfigMapper.MapDocker(dto);

        result.Should().NotBeNull();
        result!.Container.Should().Be("my-container");
        result.User.Should().Be("root");
        result.WorkingDirectory.Should().Be("/app");
        result.Environment.Should().ContainKey("KEY");
        result.Interactive.Should().BeTrue();
        result.Tty.Should().BeTrue();
        result.Privileged.Should().BeFalse();
        result.Host.Should().Be("docker.local");
        result.ExtraArgs.Should().Contain("--rm");
        result.Disabled.Should().BeFalse();
    }

    [Fact]
    public void MapSsh_WithNull_ReturnsNull()
    {
        var result = ExecutionConfigMapper.MapSsh(null);
        result.Should().BeNull();
    }

    [Fact]
    public void MapSsh_WithEmptyDto_ReturnsNull()
    {
        var dto = new SshDto();
        var result = ExecutionConfigMapper.MapSsh(dto);
        result.Should().BeNull();
    }

    [Fact]
    public void MapSsh_WithHost_ReturnsConfig()
    {
        var dto = new SshDto { Host = "server.example.com" };

        var result = ExecutionConfigMapper.MapSsh(dto);

        result.Should().NotBeNull();
        result!.Host.Should().Be("server.example.com");
        result.Port.Should().Be(22); // Default
    }

    [Fact]
    public void MapSsh_WithAllProperties_MapsCorrectly()
    {
        var dto = new SshDto
        {
            Host = "server.example.com",
            User = "deploy",
            Port = 2222,
            PrivateKeyPath = "~/.ssh/id_rsa",
            WorkingDirectory = "/home/deploy",
            Environment = new Dictionary<string, string> { ["PATH"] = "/usr/bin" },
            StrictHostKeyChecking = false,
            ExtraArgs = ["-v"],
            ConnectionTimeoutSeconds = 60,
            Disabled = false
        };

        var result = ExecutionConfigMapper.MapSsh(dto);

        result.Should().NotBeNull();
        result!.Host.Should().Be("server.example.com");
        result.User.Should().Be("deploy");
        result.Port.Should().Be(2222);
        result.PrivateKeyPath.Should().Be("~/.ssh/id_rsa");
        result.WorkingDirectory.Should().Be("/home/deploy");
        result.StrictHostKeyChecking.Should().BeFalse();
        result.ConnectionTimeoutSeconds.Should().Be(60);
    }
}
