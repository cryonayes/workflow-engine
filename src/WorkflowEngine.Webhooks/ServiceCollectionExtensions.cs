using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Webhooks.Providers;

namespace WorkflowEngine.Webhooks;

/// <summary>
/// Extension methods for configuring webhook services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds webhook notification services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebhookServices(this IServiceCollection services)
    {
        // Register HTTP client for webhook providers
        services.AddHttpClient<DiscordWebhookProvider>();
        services.AddHttpClient<SlackWebhookProvider>();
        services.AddHttpClient<TelegramWebhookProvider>();
        services.AddHttpClient<GenericHttpWebhookProvider>();

        // Register providers
        services.AddSingleton<IWebhookProvider, DiscordWebhookProvider>();
        services.AddSingleton<IWebhookProvider, SlackWebhookProvider>();
        services.AddSingleton<IWebhookProvider, TelegramWebhookProvider>();
        services.AddSingleton<IWebhookProvider, GenericHttpWebhookProvider>();

        // Register notification handler
        services.AddSingleton<IWebhookNotifier, WebhookNotificationHandler>();

        return services;
    }
}
