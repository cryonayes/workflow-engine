using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Events;

namespace WorkflowEngine.Console.Commands;

/// <summary>
/// CLI commands for managing the scheduler daemon.
/// </summary>
public static class DaemonCommands
{
    /// <summary>
    /// Creates the daemon command group.
    /// </summary>
    public static Command CreateCommand()
    {
        var daemonCommand = new Command("daemon", "Manage the workflow scheduler daemon");

        daemonCommand.AddCommand(CreateStartCommand());
        daemonCommand.AddCommand(CreateRunCommand());

        return daemonCommand;
    }

    private static Command CreateStartCommand()
    {
        var storagePathOption = new Option<string?>(
            ["--storage-path", "-s"],
            "Path to the schedules storage file");

        var command = new Command("start", "Start the scheduler daemon (deprecated, use 'run')")
        {
            storagePathOption
        };

        command.SetHandler(async (context) =>
        {
            var storagePath = context.ParseResult.GetValueForOption(storagePathOption);
            await RunDaemonAsync(storagePath, context.GetCancellationToken());
        });

        return command;
    }

    private static Command CreateRunCommand()
    {
        var storagePathOption = new Option<string?>(
            ["--storage-path", "-s"],
            "Path to the schedules storage file");

        var command = new Command("run", "Run the scheduler in foreground")
        {
            storagePathOption
        };

        command.SetHandler(async (context) =>
        {
            var storagePath = context.ParseResult.GetValueForOption(storagePathOption);
            await RunDaemonAsync(storagePath, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task RunDaemonAsync(string? storagePath, CancellationToken cancellationToken)
    {
        await using var services = CommandHelpers.BuildSchedulingServices(storagePath);
        var scheduler = services.GetRequiredService<IScheduler>();

        // Subscribe to events for logging
        scheduler.OnSchedulerEvent += OnSchedulerEvent;

        AnsiConsole.MarkupLine("[blue]Starting workflow scheduler daemon...[/]");
        AnsiConsole.MarkupLine("[grey]Press Ctrl+C to stop[/]");
        AnsiConsole.WriteLine();

        try
        {
            await scheduler.StartAsync(cancellationToken);

            // Wait until cancellation
            var tcs = new TaskCompletionSource();
            using var registration = cancellationToken.Register(() => tcs.TrySetResult());
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Shutting down scheduler...[/]");
            await scheduler.StopAsync();
            AnsiConsole.MarkupLine("[green]Scheduler stopped[/]");
        }
    }

    private static void OnSchedulerEvent(object? sender, SchedulerEvent evt)
    {
        var timestamp = evt.Timestamp.ToString("HH:mm:ss");

        switch (evt)
        {
            case SchedulerStartedEvent started:
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [green]Scheduler started[/] with {started.ActiveScheduleCount} active schedule(s)");
                break;

            case SchedulerStoppedEvent stopped:
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [yellow]Scheduler stopped:[/] {Markup.Escape(stopped.Reason)}");
                break;

            case ScheduledRunTriggeredEvent triggered:
                var triggerType = triggered.IsManual ? "[cyan]manual[/]" : "[blue]scheduled[/]";
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [blue]Run triggered[/] ({triggerType}): {Markup.Escape(triggered.ScheduleId)} - {Markup.Escape(Path.GetFileName(triggered.WorkflowPath))}");
                break;

            case ScheduledRunCompletedEvent completed:
                var statusColor = Rendering.StatusColorProvider.GetColor(completed.Status);
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [{statusColor}]Run completed[/]: {Markup.Escape(completed.ScheduleId)} - {completed.Status} ({completed.Duration.TotalSeconds:F1}s)");
                if (!string.IsNullOrEmpty(completed.ErrorMessage))
                {
                    AnsiConsole.MarkupLine($"           [red]Error:[/] {Markup.Escape(completed.ErrorMessage)}");
                }
                break;

            case ScheduleAddedEvent added:
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [green]Schedule added:[/] {Markup.Escape(added.ScheduleId)} - {Markup.Escape(added.CronExpression)}");
                break;

            case ScheduleRemovedEvent removed:
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [yellow]Schedule removed:[/] {Markup.Escape(removed.ScheduleId)}");
                break;

            case ScheduleEnabledEvent enabled:
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [green]Schedule enabled:[/] {Markup.Escape(enabled.ScheduleId)}");
                break;

            case ScheduleDisabledEvent disabled:
                AnsiConsole.MarkupLine($"[grey]{timestamp}[/] [yellow]Schedule disabled:[/] {Markup.Escape(disabled.ScheduleId)}");
                break;
        }
    }

}
