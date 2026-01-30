using Microsoft.Extensions.Logging;
using WorkflowEngine.Core;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Parsing.Dtos;
using WorkflowEngine.Parsing.Mappers;
using WorkflowEngine.Parsing.TypeParsers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WorkflowEngine.Parsing;

/// <summary>
/// Parses workflow definitions from YAML format.
/// </summary>
public sealed class YamlWorkflowParser : IWorkflowParser
{
    private readonly IDeserializer _deserializer;
    private readonly IWorkflowValidator _validator;
    private readonly ILogger<YamlWorkflowParser> _logger;

    /// <summary>
    /// Initializes a new instance with required dependencies.
    /// </summary>
    public YamlWorkflowParser(IWorkflowValidator validator, ILogger<YamlWorkflowParser> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <inheritdoc />
    public Workflow Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _logger.LogDebug("Parsing workflow from YAML string ({Length} chars)", content.Length);

        try
        {
            var dto = _deserializer.Deserialize<WorkflowDto>(content);

            if (dto is null)
            {
                throw new WorkflowParsingException("Failed to parse YAML: empty or invalid document");
            }

            var workflow = MapToWorkflow(dto);

            var validation = _validator.Validate(workflow);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Workflow validation failed with {ErrorCount} errors", validation.Errors.Count);
                throw new WorkflowParsingException(
                    "Workflow validation failed",
                    validation.Errors.ToList());
            }

            _logger.LogInformation("Successfully parsed workflow '{Name}' with {TaskCount} tasks",
                workflow.Name, workflow.Tasks.Count);

            return workflow;
        }
        catch (WorkflowParsingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse workflow YAML");
            throw new WorkflowParsingException($"YAML parsing error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Workflow ParseFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Workflow file not found: {filePath}", filePath);
        }

        _logger.LogDebug("Reading workflow from file: {FilePath}", filePath);
        var yaml = File.ReadAllText(filePath);
        return Parse(yaml);
    }

    private static Workflow MapToWorkflow(WorkflowDto dto)
    {
        var workflowShell = dto.Shell;
        return new Workflow
        {
            Name = dto.Name ?? "Unnamed Workflow",
            Description = dto.Description,
            Environment = dto.Environment ?? [],
            WorkingDirectory = dto.WorkingDirectory,
            Tasks = dto.Tasks?.Select(t => MapToTask(t, workflowShell)).ToList() ?? [],
            DefaultTimeoutMs = dto.DefaultTimeoutMs ?? Defaults.TimeoutMs,
            MaxParallelism = dto.MaxParallelism ?? -1,
            Webhooks = dto.Webhooks?.Select(MapToWebhook).ToList() ?? [],
            Docker = ExecutionConfigMapper.MapDocker(dto.Docker),
            Ssh = ExecutionConfigMapper.MapSsh(dto.Ssh),
            Watch = WatchConfigMapper.Map(dto.Watch),
            Shell = workflowShell
        };
    }

    private static WorkflowTask MapToTask(TaskDto dto, string? workflowShell)
    {
        return new WorkflowTask
        {
            Id = dto.Id ?? throw new WorkflowParsingException("Task ID is required"),
            Name = dto.Name,
            Run = dto.Run ?? throw new WorkflowParsingException($"Task '{dto.Id}' requires a 'run' command"),
            Shell = dto.Shell ?? workflowShell,
            WorkingDirectory = dto.WorkingDirectory,
            Environment = dto.Environment ?? [],
            If = dto.If,
            Input = MapToTaskInput(dto.Input),
            Output = MapToTaskOutputConfig(dto.Output),
            TimeoutMs = dto.TimeoutMs,
            ContinueOnError = dto.ContinueOnError ?? false,
            RetryCount = dto.RetryCount ?? 0,
            RetryDelayMs = dto.RetryDelayMs ?? Defaults.RetryDelayMs,
            DependsOn = dto.DependsOn ?? [],
            Matrix = MatrixConfigMapper.Map(dto.Matrix),
            Docker = ExecutionConfigMapper.MapDocker(dto.Docker),
            Ssh = ExecutionConfigMapper.MapSsh(dto.Ssh)
        };
    }

    private static TaskInput? MapToTaskInput(TaskInputDto? dto)
    {
        if (dto is null)
            return null;

        return new TaskInput
        {
            Type = InputTypeParser.Instance.Parse(dto.Type),
            Value = dto.Value,
            FilePath = dto.FilePath
        };
    }

    private static TaskOutputConfig? MapToTaskOutputConfig(TaskOutputDto? dto)
    {
        if (dto is null)
            return null;

        return new TaskOutputConfig
        {
            Type = OutputTypeParser.Instance.Parse(dto.Type),
            FilePath = dto.FilePath,
            CaptureStderr = dto.CaptureStderr ?? true,
            MaxSizeBytes = dto.MaxSizeBytes ?? Defaults.MaxOutputSizeBytes
        };
    }

    private static WebhookConfig MapToWebhook(WebhookDto dto)
    {
        return new WebhookConfig
        {
            Provider = dto.Provider ?? throw new WorkflowParsingException("Webhook provider is required"),
            Url = dto.Url ?? throw new WorkflowParsingException("Webhook URL is required"),
            Name = dto.Name,
            Events = dto.Events?.Select(e => WebhookEventTypeParser.Instance.Parse(e)).ToList()
                ?? [WebhookEventType.WorkflowCompleted, WebhookEventType.WorkflowFailed],
            Headers = dto.Headers ?? new Dictionary<string, string>(),
            Options = dto.Options ?? new Dictionary<string, string>(),
            TimeoutMs = dto.TimeoutMs ?? 10000,
            RetryCount = dto.RetryCount ?? 2
        };
    }
}
