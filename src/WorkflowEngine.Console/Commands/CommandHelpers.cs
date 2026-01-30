using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Scheduling;
using WorkflowEngine.Triggers;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Console.Commands;

/// <summary>
/// Shared helper methods for CLI commands.
/// </summary>
internal static class CommandHelpers
{
    /// <summary>
    /// Parses environment variable arguments in "KEY=VALUE" format.
    /// </summary>
    /// <param name="envVars">Array of environment variable strings.</param>
    /// <returns>Dictionary of key-value pairs.</returns>
    public static Dictionary<string, string> ParseEnvironmentVariables(string[] envVars) =>
        envVars
            .Select(e => e.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

    /// <summary>
    /// Builds a service provider with scheduling services.
    /// </summary>
    /// <param name="storagePath">Optional storage path for schedules.</param>
    /// <returns>Configured service provider.</returns>
    public static ServiceProvider BuildSchedulingServices(string? storagePath = null) =>
        new ServiceCollection()
            .AddSchedulingServices(storagePath)
            .BuildServiceProvider();

    /// <summary>
    /// Builds a service provider with trigger services.
    /// </summary>
    /// <param name="verbose">Enable verbose logging.</param>
    /// <returns>Configured service provider.</returns>
    public static ServiceProvider BuildTriggerServices(bool verbose) =>
        new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Warning))
            .AddTriggerServices()
            .BuildServiceProvider();

    /// <summary>
    /// Builds a service provider with full trigger services including scheduling.
    /// </summary>
    /// <param name="config">Trigger configuration.</param>
    /// <param name="verbose">Enable verbose logging.</param>
    /// <returns>Configured service provider.</returns>
    public static ServiceProvider BuildFullTriggerServices(TriggerConfig config, bool verbose) =>
        new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Warning))
            .AddSchedulingServices()
            .AddTriggerServices()
            .AddTriggerService(config)
            .BuildServiceProvider();

    /// <summary>
    /// Truncates text to a maximum length with ellipsis.
    /// </summary>
    public static string Truncate(string text, int maxLength) =>
        text.Length > maxLength ? text[..(maxLength - 3)] + "..." : text;
}
