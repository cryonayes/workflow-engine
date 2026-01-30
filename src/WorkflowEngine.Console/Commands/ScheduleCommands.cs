using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Console.Commands;

/// <summary>
/// CLI commands for managing workflow schedules.
/// </summary>
public static class ScheduleCommands
{
    /// <summary>
    /// Creates the schedule command group.
    /// </summary>
    public static Command CreateCommand()
    {
        var scheduleCommand = new Command("schedule", "Manage workflow schedules");

        scheduleCommand.AddCommand(CreateAddCommand());
        scheduleCommand.AddCommand(CreateRemoveCommand());
        scheduleCommand.AddCommand(CreateListCommand());
        scheduleCommand.AddCommand(CreateEnableCommand());
        scheduleCommand.AddCommand(CreateDisableCommand());
        scheduleCommand.AddCommand(CreateTriggerCommand());
        scheduleCommand.AddCommand(CreateShowCommand());

        return scheduleCommand;
    }

    private static Command CreateAddCommand()
    {
        var workflowArg = new Argument<FileInfo>(
            "workflow",
            "Path to the workflow YAML file");

        var cronOption = new Option<string>(
            ["--cron", "-c"],
            "Cron expression for the schedule (e.g., '0 2 * * *' for daily at 2 AM)")
        {
            IsRequired = true
        };

        var nameOption = new Option<string?>(
            ["--name", "-n"],
            "Optional name for the schedule");

        var disabledOption = new Option<bool>(
            ["--disabled", "-d"],
            "Create the schedule in disabled state");

        var envOption = new Option<string[]>(
            ["--env", "-e"],
            "Environment variables to pass (format: NAME=VALUE)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("add", "Add a new workflow schedule")
        {
            workflowArg,
            cronOption,
            nameOption,
            disabledOption,
            envOption
        };

        command.SetHandler(async (context) =>
        {
            var workflow = context.ParseResult.GetValueForArgument(workflowArg);
            var cron = context.ParseResult.GetValueForOption(cronOption)!;
            var name = context.ParseResult.GetValueForOption(nameOption);
            var disabled = context.ParseResult.GetValueForOption(disabledOption);
            var envVars = context.ParseResult.GetValueForOption(envOption) ?? [];

            await AddScheduleAsync(workflow, cron, name, disabled, envVars);
        });

        return command;
    }

    private static Command CreateRemoveCommand()
    {
        var idArg = new Argument<string>(
            "schedule-id",
            "The schedule ID to remove");

        var command = new Command("remove", "Remove a schedule")
        {
            idArg
        };

        command.SetHandler(async (context) =>
        {
            var id = context.ParseResult.GetValueForArgument(idArg);
            await RemoveScheduleAsync(id);
        });

        return command;
    }

    private static Command CreateListCommand()
    {
        var enabledOption = new Option<bool>(
            ["--enabled", "-e"],
            "Show only enabled schedules");

        var command = new Command("list", "List all schedules")
        {
            enabledOption
        };

        command.SetHandler(async (context) =>
        {
            var enabledOnly = context.ParseResult.GetValueForOption(enabledOption);
            await ListSchedulesAsync(enabledOnly);
        });

        return command;
    }

    private static Command CreateEnableCommand()
    {
        var idArg = new Argument<string>(
            "schedule-id",
            "The schedule ID to enable");

        var command = new Command("enable", "Enable a schedule")
        {
            idArg
        };

        command.SetHandler(async (context) =>
        {
            var id = context.ParseResult.GetValueForArgument(idArg);
            await EnableScheduleAsync(id);
        });

        return command;
    }

    private static Command CreateDisableCommand()
    {
        var idArg = new Argument<string>(
            "schedule-id",
            "The schedule ID to disable");

        var command = new Command("disable", "Disable a schedule")
        {
            idArg
        };

        command.SetHandler(async (context) =>
        {
            var id = context.ParseResult.GetValueForArgument(idArg);
            await DisableScheduleAsync(id);
        });

        return command;
    }

    private static Command CreateTriggerCommand()
    {
        var idArg = new Argument<string>(
            "schedule-id",
            "The schedule ID to trigger");

        var command = new Command("trigger", "Manually trigger a schedule now")
        {
            idArg
        };

        command.SetHandler(async (context) =>
        {
            var id = context.ParseResult.GetValueForArgument(idArg);
            await TriggerScheduleAsync(id);
        });

        return command;
    }

    private static Command CreateShowCommand()
    {
        var idArg = new Argument<string>(
            "schedule-id",
            "The schedule ID to show");

        var command = new Command("show", "Show details of a schedule")
        {
            idArg
        };

        command.SetHandler(async (context) =>
        {
            var id = context.ParseResult.GetValueForArgument(idArg);
            await ShowScheduleAsync(id);
        });

        return command;
    }

    private static async Task AddScheduleAsync(
        FileInfo workflow,
        string cron,
        string? name,
        bool disabled,
        string[] envVars)
    {
        if (!workflow.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Workflow file not found: {workflow.FullName}");
            return;
        }

        await using var services = CommandHelpers.BuildSchedulingServices();
        var scheduler = services.GetRequiredService<IScheduler>();
        var cronParser = services.GetRequiredService<ICronParser>();

        if (!cronParser.IsValid(cron))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid cron expression: {cron}");
            return;
        }

        var schedule = new WorkflowSchedule
        {
            Id = GenerateScheduleId(),
            WorkflowPath = workflow.FullName,
            CronExpression = cron,
            Name = name,
            Enabled = !disabled,
            InputParameters = CommandHelpers.ParseEnvironmentVariables(envVars)
        };

        var saved = await scheduler.AddScheduleAsync(schedule);

        AnsiConsole.MarkupLine($"[green]Schedule created successfully[/]");
        AnsiConsole.MarkupLine($"  [bold]ID:[/] {saved.Id}");
        AnsiConsole.MarkupLine($"  [bold]Workflow:[/] {Markup.Escape(workflow.Name)}");
        AnsiConsole.MarkupLine($"  [bold]Cron:[/] {cron} ({cronParser.GetDescription(cron)})");
        AnsiConsole.MarkupLine($"  [bold]Status:[/] {(saved.Enabled ? "[green]Enabled[/]" : "[yellow]Disabled[/]")}");

        if (saved.NextRunAt.HasValue)
        {
            AnsiConsole.MarkupLine($"  [bold]Next run:[/] {saved.NextRunAt.Value:yyyy-MM-dd HH:mm:ss}");
        }
    }

    private static async Task RemoveScheduleAsync(string id)
    {
        await using var services = CommandHelpers.BuildSchedulingServices();
        var storage = services.GetRequiredService<IScheduleStorage>();

        var removed = await storage.DeleteAsync(id);

        if (removed)
        {
            AnsiConsole.MarkupLine($"[green]Schedule '{id}' removed successfully[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Schedule '{id}' not found[/]");
        }
    }

    private static async Task ListSchedulesAsync(bool enabledOnly)
    {
        await using var services = CommandHelpers.BuildSchedulingServices();
        var storage = services.GetRequiredService<IScheduleStorage>();
        var cronParser = services.GetRequiredService<ICronParser>();

        var schedules = enabledOnly
            ? await storage.GetEnabledAsync()
            : await storage.GetAllAsync();

        if (schedules.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No schedules found[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Workflow");
        table.AddColumn("Cron");
        table.AddColumn("Status");
        table.AddColumn("Next Run");

        foreach (var schedule in schedules)
        {
            var status = schedule.Enabled
                ? "[green]Enabled[/]"
                : "[yellow]Disabled[/]";

            var nextRun = schedule.NextRunAt.HasValue
                ? schedule.NextRunAt.Value.ToString("MM-dd HH:mm")
                : "-";

            var workflowName = Path.GetFileName(schedule.WorkflowPath);

            table.AddRow(
                Markup.Escape(schedule.Id),
                Markup.Escape(schedule.Name ?? "-"),
                Markup.Escape(workflowName),
                Markup.Escape(schedule.CronExpression),
                status,
                nextRun);
        }

        AnsiConsole.Write(table);
    }

    private static async Task EnableScheduleAsync(string id)
    {
        await using var services = CommandHelpers.BuildSchedulingServices();
        var scheduler = services.GetRequiredService<IScheduler>();

        try
        {
            await scheduler.EnableScheduleAsync(id);
            AnsiConsole.MarkupLine($"[green]Schedule '{id}' enabled[/]");
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private static async Task DisableScheduleAsync(string id)
    {
        await using var services = CommandHelpers.BuildSchedulingServices();
        var scheduler = services.GetRequiredService<IScheduler>();

        try
        {
            await scheduler.DisableScheduleAsync(id);
            AnsiConsole.MarkupLine($"[yellow]Schedule '{id}' disabled[/]");
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private static async Task TriggerScheduleAsync(string id)
    {
        await using var services = CommandHelpers.BuildSchedulingServices();
        var scheduler = services.GetRequiredService<IScheduler>();

        try
        {
            var runId = await scheduler.TriggerScheduleAsync(id);
            AnsiConsole.MarkupLine($"[green]Schedule '{id}' triggered[/]");
            AnsiConsole.MarkupLine($"  [bold]Run ID:[/] {runId}");
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private static async Task ShowScheduleAsync(string id)
    {
        await using var services = CommandHelpers.BuildSchedulingServices();
        var storage = services.GetRequiredService<IScheduleStorage>();
        var cronParser = services.GetRequiredService<ICronParser>();

        var schedule = await storage.GetAsync(id);

        if (schedule == null)
        {
            AnsiConsole.MarkupLine($"[yellow]Schedule '{id}' not found[/]");
            return;
        }

        var panel = new Panel(new Rows(
            new Markup($"[bold]ID:[/] {Markup.Escape(schedule.Id)}"),
            new Markup($"[bold]Name:[/] {Markup.Escape(schedule.Name ?? "-")}"),
            new Markup($"[bold]Workflow:[/] {Markup.Escape(schedule.WorkflowPath)}"),
            new Markup($"[bold]Cron:[/] {Markup.Escape(schedule.CronExpression)}"),
            new Markup($"[bold]Description:[/] {Markup.Escape(cronParser.GetDescription(schedule.CronExpression))}"),
            new Markup($"[bold]Status:[/] {(schedule.Enabled ? "[green]Enabled[/]" : "[yellow]Disabled[/]")}"),
            new Markup($"[bold]Created:[/] {schedule.CreatedAt:yyyy-MM-dd HH:mm:ss}"),
            new Markup($"[bold]Last Run:[/] {(schedule.LastRunAt.HasValue ? schedule.LastRunAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-")}"),
            new Markup($"[bold]Next Run:[/] {(schedule.NextRunAt.HasValue ? schedule.NextRunAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-")}")
        ))
        {
            Header = new PanelHeader($"Schedule: {schedule.Id}")
        };

        AnsiConsole.Write(panel);

        if (schedule.InputParameters.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Input Parameters:[/]");
            foreach (var param in schedule.InputParameters)
            {
                AnsiConsole.MarkupLine($"  {Markup.Escape(param.Key)}={Markup.Escape(param.Value)}");
            }
        }
    }

    private static string GenerateScheduleId() =>
        $"sch-{Guid.NewGuid():N}"[..12];

}
