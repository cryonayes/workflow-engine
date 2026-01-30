using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Scheduling.Storage;

/// <summary>
/// JSON file-based implementation of schedule storage.
/// Thread-safe using file locking.
/// </summary>
public sealed class JsonFileScheduleStorage : IScheduleStorage
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Creates a new JSON file storage instance.
    /// </summary>
    /// <param name="filePath">Path to the JSON file. Defaults to ~/.workflow-engine/schedules.json</param>
    public JsonFileScheduleStorage(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultPath();
        EnsureDirectoryExists();
    }

    private static string GetDefaultPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".workflow-engine", "schedules.json");
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowSchedule?> GetAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);

        var schedules = await LoadSchedulesAsync(cancellationToken);
        return schedules.FirstOrDefault(s => s.Id.Equals(scheduleId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowSchedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await LoadSchedulesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowSchedule>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var schedules = await LoadSchedulesAsync(cancellationToken);
        return schedules.Where(s => s.Enabled).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var schedules = await LoadSchedulesInternalAsync(cancellationToken);
            var existingIndex = schedules.FindIndex(s =>
                s.Id.Equals(schedule.Id, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
                schedules[existingIndex] = schedule;
            else
                schedules.Add(schedule);

            await SaveSchedulesInternalAsync(schedules, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var schedules = await LoadSchedulesInternalAsync(cancellationToken);
            var removed = schedules.RemoveAll(s =>
                s.Id.Equals(scheduleId, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
                await SaveSchedulesInternalAsync(schedules, cancellationToken);

            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateRunTimesAsync(
        string scheduleId,
        DateTimeOffset lastRun,
        DateTimeOffset? nextRun,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var schedules = await LoadSchedulesInternalAsync(cancellationToken);
            var index = schedules.FindIndex(s =>
                s.Id.Equals(scheduleId, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                schedules[index] = schedules[index].With(lastRunAt: lastRun, nextRunAt: nextRun);
                await SaveSchedulesInternalAsync(schedules, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyList<WorkflowSchedule>> LoadSchedulesAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await LoadSchedulesInternalAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<WorkflowSchedule>> LoadSchedulesInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
                return [];

            return JsonSerializer.Deserialize<List<WorkflowSchedule>>(json, JsonOptions) ?? [];
        }
        catch (FileNotFoundException)
        {
            // File doesn't exist yet, start fresh
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist yet, start fresh
            return [];
        }
        catch (JsonException)
        {
            // File is corrupted, start fresh
            return [];
        }
    }

    private async Task SaveSchedulesInternalAsync(List<WorkflowSchedule> schedules, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(schedules, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }
}
