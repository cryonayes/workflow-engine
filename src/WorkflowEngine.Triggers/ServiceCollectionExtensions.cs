using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Listeners;
using WorkflowEngine.Triggers.Matching;
using WorkflowEngine.Triggers.Models;
using WorkflowEngine.Triggers.Services;
using WorkflowEngine.Triggers.Storage;

namespace WorkflowEngine.Triggers;

/// <summary>
/// Extension methods for registering trigger services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core trigger services to the service collection.
    /// </summary>
    public static IServiceCollection AddTriggerServices(this IServiceCollection services)
    {
        // Storage and validation
        services.AddSingleton<ITriggerStorage, YamlTriggerStorage>();
        services.AddSingleton<ITriggerConfigValidator, TriggerConfigValidator>();

        // Template resolution
        services.AddSingleton<ITemplateResolver, TemplateResolver>();

        // Matchers
        services.AddSingleton<ITypedTriggerMatcher, CommandMatcher>();
        services.AddSingleton<ITypedTriggerMatcher, PatternMatcher>();
        services.AddSingleton<ITypedTriggerMatcher, KeywordMatcher>();
        services.AddSingleton<ITriggerMatcher, TriggerMatcher>();

        // Dispatcher
        services.AddSingleton<ITriggerDispatcher, TriggerDispatcher>();

        // Base HTTP client factory (listeners will use named clients)
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Adds a configured trigger service to the service collection.
    /// </summary>
    public static IServiceCollection AddTriggerService(
        this IServiceCollection services,
        TriggerConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        services.AddSingleton(config);
        services.AddListeners(config);
        services.AddSingleton<ITriggerService, TriggerService>();

        return services;
    }

    private static IServiceCollection AddListeners(
        this IServiceCollection services,
        TriggerConfig config)
    {
        var enabledSources = GetEnabledSources(config);

        AddTelegramListenerIfEnabled(services, config, enabledSources);
        AddDiscordListenerIfEnabled(services, config, enabledSources);
        AddSlackListenerIfEnabled(services, config, enabledSources);
        AddHttpListenerIfNeeded(services, config, enabledSources);

        return services;
    }

    private static HashSet<TriggerSource> GetEnabledSources(TriggerConfig config) =>
        config.Triggers
            .Where(t => t.Enabled)
            .SelectMany(t => t.Sources)
            .ToHashSet();

    private static void AddTelegramListenerIfEnabled(
        IServiceCollection services,
        TriggerConfig config,
        HashSet<TriggerSource> enabledSources)
    {
        if (!enabledSources.Contains(TriggerSource.Telegram))
            return;

        if (string.IsNullOrEmpty(config.Credentials.Telegram?.BotToken))
            return;

        services.AddSingleton<ITriggerListener>(sp =>
            new TelegramTriggerListener(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(TelegramTriggerListener)),
                config.Credentials.Telegram!.BotToken,
                sp.GetRequiredService<ILogger<TelegramTriggerListener>>()));
    }

    private static void AddDiscordListenerIfEnabled(
        IServiceCollection services,
        TriggerConfig config,
        HashSet<TriggerSource> enabledSources)
    {
        if (!enabledSources.Contains(TriggerSource.Discord))
            return;

        if (string.IsNullOrEmpty(config.Credentials.Discord?.BotToken))
            return;

        services.AddSingleton<ITriggerListener>(sp =>
            new DiscordTriggerListener(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(DiscordTriggerListener)),
                config.Credentials.Discord!.BotToken,
                sp.GetRequiredService<ILogger<DiscordTriggerListener>>()));
    }

    private static void AddSlackListenerIfEnabled(
        IServiceCollection services,
        TriggerConfig config,
        HashSet<TriggerSource> enabledSources)
    {
        if (!enabledSources.Contains(TriggerSource.Slack))
            return;

        var slackCreds = config.Credentials.Slack;
        if (string.IsNullOrEmpty(slackCreds?.AppToken) || string.IsNullOrEmpty(slackCreds?.SigningSecret))
            return;

        services.AddSingleton(sp =>
            new SlackTriggerListener(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SlackTriggerListener)),
                slackCreds.AppToken,
                slackCreds.SigningSecret,
                sp.GetRequiredService<ILogger<SlackTriggerListener>>()));

        services.AddSingleton<ITriggerListener>(sp => sp.GetRequiredService<SlackTriggerListener>());
        services.AddSingleton<ISlackEventProcessor>(sp => sp.GetRequiredService<SlackTriggerListener>());
    }

    private static void AddHttpListenerIfNeeded(
        IServiceCollection services,
        TriggerConfig config,
        HashSet<TriggerSource> enabledSources)
    {
        // HTTP listener is needed for HTTP triggers or Slack events
        if (!enabledSources.Contains(TriggerSource.Http) && !enabledSources.Contains(TriggerSource.Slack))
            return;

        services.AddSingleton<ITriggerListener>(sp =>
            new HttpTriggerListener(
                config.HttpServer,
                sp.GetService<ISlackEventProcessor>(),
                sp.GetRequiredService<ILogger<HttpTriggerListener>>()));
    }
}
