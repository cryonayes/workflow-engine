using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using WorkflowEngine.Scheduling;
using WorkflowEngine.Triggers;
using WorkflowEngine.Triggers.Abstractions;
using WorkflowEngine.Triggers.Events;
using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Console.Commands;

/// <summary>
/// CLI commands for managing trigger service.
/// </summary>
public static class TriggerCommands
{
    /// <summary>
    /// Creates the trigger command group.
    /// </summary>
    public static Command CreateCommand()
    {
        var command = new Command("trigger", "Manage trigger service for receiving inbound messages");

        command.AddCommand(CreateRunCommand());
        command.AddCommand(CreateValidateCommand());
        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateTestCommand());

        return command;
    }

    private static Command CreateRunCommand()
    {
        var configOption = new Option<FileInfo?>(["--config", "-c"], "Path to triggers.yaml");
        var verboseOption = new Option<bool>(["--verbose", "-v"], "Enable verbose output");

        var command = new Command("run", "Run the trigger service daemon") { configOption, verboseOption };

        command.SetHandler(async context =>
        {
            var configFile = context.ParseResult.GetValueForOption(configOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            context.ExitCode = await RunTriggerServiceAsync(configFile, verbose, context.GetCancellationToken());
        });

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var configArg = new Argument<FileInfo>("config", "Path to triggers.yaml");
        var command = new Command("validate", "Validate a trigger configuration file") { configArg };

        command.SetHandler(async context =>
        {
            var configFile = context.ParseResult.GetValueForArgument(configArg);
            context.ExitCode = await ValidateConfigAsync(configFile, context.GetCancellationToken());
        });

        return command;
    }

    private static Command CreateListCommand()
    {
        var configOption = new Option<FileInfo?>(["--config", "-c"], "Path to triggers.yaml");
        var command = new Command("list", "List configured triggers") { configOption };

        command.SetHandler(async context =>
        {
            var configFile = context.ParseResult.GetValueForOption(configOption);
            context.ExitCode = await ListTriggersAsync(configFile, context.GetCancellationToken());
        });

        return command;
    }

    private static Command CreateTestCommand()
    {
        var messageArg = new Argument<string>("message", "Message text to test");
        var sourceOption = new Option<string>(["--source", "-s"], () => "telegram", "Source platform");
        var configOption = new Option<FileInfo?>(["--config", "-c"], "Path to triggers.yaml");

        var command = new Command("test", "Test a message against trigger rules") { messageArg, sourceOption, configOption };

        command.SetHandler(async context =>
        {
            var message = context.ParseResult.GetValueForArgument(messageArg);
            var source = context.ParseResult.GetValueForOption(sourceOption) ?? "telegram";
            var configFile = context.ParseResult.GetValueForOption(configOption);
            context.ExitCode = await TestMessageAsync(message, source, configFile, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> RunTriggerServiceAsync(FileInfo? configFile, bool verbose, CancellationToken ct)
    {
        await using var services = CommandHelpers.BuildTriggerServices(verbose);
        var storage = services.GetRequiredService<ITriggerStorage>();
        var validator = services.GetRequiredService<ITriggerConfigValidator>();

        var configPath = configFile?.FullName ?? storage.GetDefaultConfigPath();

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Configuration file not found: {configPath}");
            return 1;
        }

        TriggerConfig config;
        try
        {
            config = await storage.LoadAsync(configPath, ct);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading config:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        var validation = validator.Validate(config);
        if (!validation.IsValid)
        {
            AnsiConsole.MarkupLine("[red]Configuration validation failed:[/]");
            foreach (var error in validation.Errors)
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(error)}");
            return 1;
        }

        await using var triggerServices = CommandHelpers.BuildFullTriggerServices(config, verbose);
        var triggerService = triggerServices.GetRequiredService<ITriggerService>();

        triggerService.OnTriggerEvent += (_, evt) => LogTriggerEvent(evt, verbose);

        AnsiConsole.MarkupLine("[bold blue]Starting Trigger Service[/]");
        AnsiConsole.MarkupLine($"[grey]Config: {configPath}[/]");
        AnsiConsole.MarkupLine($"[grey]Triggers: {config.Triggers.Count}[/]");
        AnsiConsole.WriteLine();

        try
        {
            await triggerService.StartAsync(ct);
            AnsiConsole.MarkupLine("[green]Trigger service running. Press Ctrl+C to stop.[/]");
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Shutting down...[/]");
        }
        finally
        {
            await triggerService.StopAsync();
            AnsiConsole.MarkupLine("[grey]Trigger service stopped[/]");
        }

        return 0;
    }

    private static async Task<int> ValidateConfigAsync(FileInfo configFile, CancellationToken ct)
    {
        if (!configFile.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {configFile.FullName}");
            return 1;
        }

        await using var services = CommandHelpers.BuildTriggerServices(false);
        var storage = services.GetRequiredService<ITriggerStorage>();
        var validator = services.GetRequiredService<ITriggerConfigValidator>();

        try
        {
            var config = await storage.LoadAsync(configFile.FullName, ct);
            var result = validator.Validate(config);

            if (result.IsValid)
            {
                AnsiConsole.MarkupLine("[green]✓ Configuration is valid[/]");
                AnsiConsole.MarkupLine($"  [bold]Triggers:[/] {config.Triggers.Count}");

                var sources = config.Triggers.SelectMany(t => t.Sources).Distinct().OrderBy(s => s.ToString());
                AnsiConsole.MarkupLine($"  [bold]Sources:[/] {string.Join(", ", sources)}");

                if (result.Warnings.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
                    foreach (var warning in result.Warnings)
                        AnsiConsole.MarkupLine($"  [yellow]•[/] {Markup.Escape(warning)}");
                }

                return 0;
            }

            AnsiConsole.MarkupLine("[red]✗ Configuration validation failed:[/]");
            foreach (var error in result.Errors)
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(error)}");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static async Task<int> ListTriggersAsync(FileInfo? configFile, CancellationToken ct)
    {
        await using var services = CommandHelpers.BuildTriggerServices(false);
        var storage = services.GetRequiredService<ITriggerStorage>();

        var configPath = configFile?.FullName ?? storage.GetDefaultConfigPath();

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[yellow]No configuration file found at:[/] {configPath}");
            return 0;
        }

        try
        {
            var config = await storage.LoadAsync(configPath, ct);

            if (config.Triggers.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No triggers configured[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Sources");
            table.AddColumn("Pattern/Keywords");
            table.AddColumn("Workflow");
            table.AddColumn("Status");

            foreach (var trigger in config.Triggers)
            {
                var sources = string.Join(", ", trigger.Sources);
                var pattern = GetPatternDisplay(trigger);
                var status = trigger.Enabled ? "[green]Enabled[/]" : "[yellow]Disabled[/]";
                var workflow = Path.GetFileName(trigger.WorkflowPath);

                table.AddRow(
                    Markup.Escape(trigger.Name),
                    trigger.Type.ToString(),
                    Markup.Escape(sources),
                    Markup.Escape(CommandHelpers.Truncate(pattern, 30)),
                    Markup.Escape(workflow),
                    status);
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static async Task<int> TestMessageAsync(string message, string sourceStr, FileInfo? configFile, CancellationToken ct)
    {
        if (!Enum.TryParse<TriggerSource>(sourceStr, true, out var source))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid source: {sourceStr}");
            AnsiConsole.MarkupLine("[grey]Valid: telegram, discord, slack, http[/]");
            return 1;
        }

        await using var services = CommandHelpers.BuildTriggerServices(false);
        var storage = services.GetRequiredService<ITriggerStorage>();
        var matcher = services.GetRequiredService<ITriggerMatcher>();
        var templateResolver = services.GetRequiredService<ITemplateResolver>();

        var configPath = configFile?.FullName ?? storage.GetDefaultConfigPath();

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Configuration not found: {configPath}");
            return 1;
        }

        try
        {
            var config = await storage.LoadAsync(configPath, ct);

            var testMessage = new IncomingMessage
            {
                MessageId = "test-" + Guid.NewGuid().ToString("N")[..8],
                Source = source,
                Text = message,
                Username = "test-user",
                UserId = "12345",
                ChannelId = "test-channel"
            };

            AnsiConsole.MarkupLine($"[bold]Testing:[/] \"{Markup.Escape(message)}\"");
            AnsiConsole.MarkupLine($"[bold]Source:[/] {source}");
            AnsiConsole.WriteLine();

            var result = matcher.Match(testMessage, config.Triggers);

            if (result is not { IsMatch: true, Rule: not null })
            {
                AnsiConsole.MarkupLine("[yellow]No matching trigger found[/]");
                return 0;
            }

            var rule = result.Rule;
            var resolvedParams = templateResolver.ResolveParameters(rule.Parameters, result.Captures, testMessage);
            var resolvedResponse = rule.ResponseTemplate is not null
                ? templateResolver.Resolve(rule.ResponseTemplate, result.Captures, testMessage)
                : null;

            AnsiConsole.MarkupLine($"[green]✓ Matched:[/] {rule.Name}");
            AnsiConsole.MarkupLine($"  [bold]Type:[/] {rule.Type}");
            AnsiConsole.MarkupLine($"  [bold]Workflow:[/] {rule.WorkflowPath}");

            if (result.Captures.Count > 0)
            {
                AnsiConsole.MarkupLine($"  [bold]Captures:[/]");
                foreach (var (key, value) in result.Captures)
                    AnsiConsole.MarkupLine($"    {Markup.Escape(key)}: {Markup.Escape(value)}");
            }

            if (resolvedParams.Count > 0)
            {
                AnsiConsole.MarkupLine($"  [bold]Parameters:[/]");
                foreach (var (key, value) in resolvedParams)
                    AnsiConsole.MarkupLine($"    {Markup.Escape(key)}: {Markup.Escape(value)}");
            }

            if (resolvedResponse is not null)
                AnsiConsole.MarkupLine($"  [bold]Response:[/] {Markup.Escape(resolvedResponse)}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static void LogTriggerEvent(TriggerEvent evt, bool verbose)
    {
        var timestamp = $"[[{evt.Timestamp:HH:mm:ss}]]";

        switch (evt)
        {
            case ListenerConnectedEvent e:
                AnsiConsole.MarkupLine($"[green]{timestamp} {e.Source} listener connected[/]");
                break;

            case ListenerDisconnectedEvent e:
                AnsiConsole.MarkupLine($"[yellow]{timestamp} {e.Source} listener disconnected{(e.Reason is not null ? $": {Markup.Escape(e.Reason)}" : "")}[/]");
                break;

            case MessageReceivedEvent e when verbose:
                AnsiConsole.MarkupLine($"[grey]{timestamp} Message from {e.Source}: {Markup.Escape(e.TextPreview)}[/]");
                break;

            case TriggerMatchedEvent e:
                AnsiConsole.MarkupLine($"[blue]{timestamp} Matched '{Markup.Escape(e.RuleName)}' from {e.Source}[/]");
                break;

            case TriggerDispatchedEvent e:
                AnsiConsole.MarkupLine($"[green]{timestamp} Dispatched: {Markup.Escape(e.WorkflowPath)} (run: {e.RunId})[/]");
                break;

            case TriggerDispatchFailedEvent e:
                AnsiConsole.MarkupLine($"[red]{timestamp} Dispatch failed for '{Markup.Escape(e.RuleName)}': {Markup.Escape(e.ErrorMessage)}[/]");
                break;

            case TriggerErrorEvent e:
                AnsiConsole.MarkupLine($"[red]{timestamp} Error in {Markup.Escape(e.Component)}: {Markup.Escape(e.ErrorMessage)}[/]");
                break;
        }
    }

    private static string GetPatternDisplay(TriggerRule trigger) =>
        trigger.Type == TriggerType.Keyword
            ? string.Join(", ", trigger.Keywords.Take(2)) + (trigger.Keywords.Count > 2 ? "..." : "")
            : trigger.Pattern ?? "-";

}
