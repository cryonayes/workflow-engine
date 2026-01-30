using Microsoft.Extensions.Logging;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Models;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers;

/// <summary>
/// Dispatches matched triggers to workflow execution.
/// </summary>
public sealed class TriggerDispatcher : ITriggerDispatcher
{
    private readonly IScheduleRunner _scheduleRunner;
    private readonly ITemplateResolver _templateResolver;
    private readonly ILogger<TriggerDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the TriggerDispatcher.
    /// </summary>
    public TriggerDispatcher(
        IScheduleRunner scheduleRunner,
        ITemplateResolver templateResolver,
        ILogger<TriggerDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(scheduleRunner);
        ArgumentNullException.ThrowIfNull(templateResolver);
        ArgumentNullException.ThrowIfNull(logger);

        _scheduleRunner = scheduleRunner;
        _templateResolver = templateResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> DispatchAsync(
        TriggerMatchResult matchResult,
        CancellationToken cancellationToken = default)
    {
        ValidateMatchResult(matchResult);

        var rule = matchResult.Rule!;
        var message = matchResult.Message!;

        var resolvedParams = _templateResolver.ResolveParameters(
            rule.Parameters,
            matchResult.Captures,
            message);

        _logger.LogInformation(
            "Dispatching workflow {WorkflowPath} for trigger {TriggerName} from {Source}",
            rule.WorkflowPath, rule.Name, message.Source);

        var request = new ManualDispatchRequest
        {
            WorkflowPath = rule.WorkflowPath,
            InputParameters = new Dictionary<string, string>(resolvedParams),
            Reason = $"Triggered by {rule.Name}",
            TriggeredBy = message.SenderDisplayName
        };

        var result = await _scheduleRunner.DispatchAsync(request, cancellationToken);

        _logger.LogInformation(
            "Workflow dispatched with run ID {RunId} for trigger {TriggerName}",
            result.RunId, rule.Name);

        return result.RunId;
    }

    private static void ValidateMatchResult(TriggerMatchResult matchResult)
    {
        ArgumentNullException.ThrowIfNull(matchResult);

        if (!matchResult.IsMatch || matchResult.Rule is null || matchResult.Message is null)
        {
            throw new ArgumentException("Cannot dispatch a non-matching result", nameof(matchResult));
        }
    }
}
