using WorkflowEngine.Core.Models;
using WorkflowEngine.Parsing.Dtos;

namespace WorkflowEngine.Parsing.Mappers;

/// <summary>
/// Maps execution configuration DTOs (Docker, SSH) to domain models.
/// </summary>
internal static class ExecutionConfigMapper
{
    /// <summary>
    /// Maps a DockerDto to a DockerConfig.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>The mapped config, or null if the DTO is null or empty.</returns>
    public static DockerConfig? MapDocker(DockerDto? dto)
    {
        if (dto is null || !dto.HasAnyValue)
            return null;

        return new DockerConfig
        {
            Container = dto.Container,
            User = dto.User,
            WorkingDirectory = dto.WorkingDirectory,
            Environment = dto.Environment ?? new Dictionary<string, string>(),
            Interactive = dto.Interactive,
            Tty = dto.Tty,
            Privileged = dto.Privileged,
            Host = dto.Host,
            ExtraArgs = dto.ExtraArgs,
            Disabled = dto.Disabled ?? false
        };
    }

    /// <summary>
    /// Maps an SshDto to an SshConfig.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>The mapped config, or null if the DTO is null or empty.</returns>
    public static SshConfig? MapSsh(SshDto? dto)
    {
        if (dto is null || !dto.HasAnyValue)
            return null;

        return new SshConfig
        {
            Host = dto.Host,
            User = dto.User,
            Port = dto.Port ?? 22,
            PrivateKeyPath = dto.PrivateKeyPath,
            WorkingDirectory = dto.WorkingDirectory,
            Environment = dto.Environment ?? new Dictionary<string, string>(),
            StrictHostKeyChecking = dto.StrictHostKeyChecking ?? true,
            ExtraArgs = dto.ExtraArgs,
            ConnectionTimeoutSeconds = dto.ConnectionTimeoutSeconds ?? 30,
            Disabled = dto.Disabled ?? false
        };
    }
}
