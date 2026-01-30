using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;
using WorkflowEngine.Parsing.Dtos;

namespace WorkflowEngine.Parsing.Mappers;

/// <summary>
/// Maps watch configuration DTOs to domain models.
/// </summary>
internal static class WatchConfigMapper
{
    /// <summary>
    /// Maps a WatchDto to a WatchConfig.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>The mapped config, or null if the DTO is null or empty.</returns>
    public static WatchConfig? Map(WatchDto? dto)
    {
        if (dto is null || !dto.HasAnyValue)
            return null;

        return new WatchConfig
        {
            Paths = dto.Paths ?? [],
            Ignore = dto.Ignore ?? [],
            Debounce = DurationParser.Parse(dto.Debounce),
            Tasks = dto.Tasks ?? [],
            Enabled = dto.Enabled ?? true,
            RunOnStart = dto.RunOnStart ?? true
        };
    }
}
