using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Models;

namespace WorkflowEngine.Console.Commands;

/// <summary>
/// CLI command for manual workflow dispatch.
/// </summary>
public static class DispatchCommand
{
    /// <summary>
    /// Creates the dispatch command.
    /// </summary>
    public static Command CreateCommand()
    {
        var workflowArg = new Argument<FileInfo>(
            "workflow",
            "Path to the workflow YAML file to dispatch");

        var envOption = new Option<string[]>(
            ["--env", "-e"],
            "Environment variables to pass (format: NAME=VALUE)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var reasonOption = new Option<string?>(
            ["--reason", "-r"],
            "Reason for the dispatch");

        var command = new Command("dispatch", "Manually dispatch a workflow for immediate execution")
        {
            workflowArg,
            envOption,
            reasonOption
        };

        command.SetHandler(async (context) =>
        {
            var workflow = context.ParseResult.GetValueForArgument(workflowArg);
            var envVars = context.ParseResult.GetValueForOption(envOption) ?? [];
            var reason = context.ParseResult.GetValueForOption(reasonOption);

            context.ExitCode = await DispatchWorkflowAsync(
                workflow,
                envVars,
                reason,
                context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> DispatchWorkflowAsync(
        FileInfo workflow,
        string[] envVars,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!workflow.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Workflow file not found: {workflow.FullName}");
            return 1;
        }

        await using var services = CommandHelpers.BuildSchedulingServices();
        var runner = services.GetRequiredService<IScheduleRunner>();

        var request = new ManualDispatchRequest
        {
            WorkflowPath = workflow.FullName,
            InputParameters = CommandHelpers.ParseEnvironmentVariables(envVars),
            Reason = reason,
            TriggeredBy = Environment.UserName
        };

        AnsiConsole.MarkupLine($"[blue]Dispatching workflow:[/] {Markup.Escape(workflow.Name)}");
        if (!string.IsNullOrEmpty(reason))
        {
            AnsiConsole.MarkupLine($"[grey]Reason: {Markup.Escape(reason)}[/]");
        }
        AnsiConsole.WriteLine();

        try
        {
            var result = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Running workflow...", async ctx =>
                {
                    return await runner.DispatchAsync(request, cancellationToken);
                });

            var statusColor = Rendering.StatusColorProvider.GetColor(result.Status);

            AnsiConsole.MarkupLine($"[{statusColor}]Workflow {result.Status}[/]");
            AnsiConsole.MarkupLine($"  [bold]Run ID:[/] {result.RunId}");
            AnsiConsole.MarkupLine($"  [bold]Duration:[/] {result.Duration?.TotalSeconds:F1}s");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                AnsiConsole.MarkupLine($"  [red]Error:[/] {Markup.Escape(result.ErrorMessage)}");
            }

            return result.Status == Core.Models.ExecutionStatus.Succeeded ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Workflow dispatch cancelled[/]");
            return 130;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

}
