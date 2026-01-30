using System.Text.RegularExpressions;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Storage;

/// <summary>
/// Validates trigger configuration.
/// </summary>
public sealed class TriggerConfigValidator : ITriggerConfigValidator
{
    /// <inheritdoc />
    public TriggerValidationResult Validate(TriggerConfig config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateCredentials(config, errors);
        ValidateTriggers(config, errors, warnings);

        return errors.Count == 0
            ? TriggerValidationResult.Success(warnings)
            : TriggerValidationResult.Failure(errors, warnings);
    }

    private static void ValidateCredentials(TriggerConfig config, List<string> errors)
    {
        var enabledSources = config.Triggers
            .Where(t => t.Enabled)
            .SelectMany(t => t.Sources)
            .ToHashSet();

        if (enabledSources.Contains(TriggerSource.Telegram) &&
            string.IsNullOrEmpty(config.Credentials.Telegram?.BotToken))
        {
            errors.Add("Telegram bot token is required for Telegram triggers");
        }

        if (enabledSources.Contains(TriggerSource.Discord) &&
            string.IsNullOrEmpty(config.Credentials.Discord?.BotToken))
        {
            errors.Add("Discord bot token is required for Discord triggers");
        }

        if (enabledSources.Contains(TriggerSource.Slack))
        {
            if (string.IsNullOrEmpty(config.Credentials.Slack?.AppToken))
                errors.Add("Slack app token is required for Slack triggers");

            if (string.IsNullOrEmpty(config.Credentials.Slack?.SigningSecret))
                errors.Add("Slack signing secret is required for Slack triggers");
        }
    }

    private static void ValidateTriggers(TriggerConfig config, List<string> errors, List<string> warnings)
    {
        var triggerNames = new HashSet<string>();

        foreach (var trigger in config.Triggers)
        {
            ValidateTrigger(trigger, triggerNames, errors, warnings);
        }
    }

    private static void ValidateTrigger(
        TriggerRule trigger,
        HashSet<string> triggerNames,
        List<string> errors,
        List<string> warnings)
    {
        // Validate name
        if (string.IsNullOrEmpty(trigger.Name))
        {
            errors.Add("Trigger name is required");
            return;
        }

        if (!triggerNames.Add(trigger.Name))
        {
            errors.Add($"Duplicate trigger name: {trigger.Name}");
        }

        // Validate sources
        if (trigger.Sources.Count == 0)
        {
            errors.Add($"Trigger '{trigger.Name}' must specify at least one source");
        }

        // Validate workflow
        if (string.IsNullOrEmpty(trigger.WorkflowPath))
        {
            errors.Add($"Trigger '{trigger.Name}' must specify a workflow path");
        }

        // Validate pattern/keywords based on type
        ValidateTriggerType(trigger, errors);

        // Validate regex pattern
        if (trigger.Type == TriggerType.Pattern && !string.IsNullOrEmpty(trigger.Pattern))
        {
            try
            {
                _ = new Regex(trigger.Pattern);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Trigger '{trigger.Name}' has invalid regex pattern: {ex.Message}");
            }
        }

        // Warnings for disabled triggers
        if (!trigger.Enabled)
        {
            warnings.Add($"Trigger '{trigger.Name}' is disabled");
        }
    }

    private static void ValidateTriggerType(TriggerRule trigger, List<string> errors)
    {
        switch (trigger.Type)
        {
            case TriggerType.Command:
            case TriggerType.Pattern:
                if (string.IsNullOrEmpty(trigger.Pattern))
                {
                    errors.Add($"Trigger '{trigger.Name}' of type {trigger.Type} must specify a pattern");
                }
                break;

            case TriggerType.Keyword:
                if (trigger.Keywords.Count == 0)
                {
                    errors.Add($"Trigger '{trigger.Name}' of type Keyword must specify at least one keyword");
                }
                break;
        }
    }
}
