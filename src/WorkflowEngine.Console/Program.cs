using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using WorkflowEngine.Console.Commands;
using WorkflowEngine.Console.Rendering;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Exceptions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Console;

/// <summary>
/// Entry point for the Workflow Engine CLI.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = BuildRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    private static RootCommand BuildRootCommand()
    {
        var workflowArgument = new Argument<FileInfo>(
            name: "workflow",
            description: "Path to the workflow YAML file")
        {
            Arity = ArgumentArity.ExactlyOne
        };

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output with detailed logging");

        var dryRunOption = new Option<bool>(
            aliases: ["--dry-run", "-n"],
            description: "Validate and plan the workflow without executing");

        var quietOption = new Option<bool>(
            aliases: ["--quiet", "-q"],
            description: "Minimal output, only show final result");

        var timeoutOption = new Option<int?>(
            aliases: ["--timeout", "-t"],
            description: "Override default timeout in seconds for all tasks");

        var workingDirOption = new Option<DirectoryInfo?>(
            aliases: ["--working-dir", "-C"],
            description: "Set the working directory for task execution");

        var envOption = new Option<string[]>(
            aliases: ["--env", "-e"],
            description: "Set environment variables (format: NAME=VALUE)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var stepOption = new Option<bool>(
            aliases: ["--step", "-s"],
            description: "Enable step mode - pause after each task for confirmation");

        var noCommandsOption = new Option<bool>(
            aliases: ["--no-commands"],
            description: "Hide the executing command in task output");

        var paramOption = new Option<string[]>(
            aliases: ["--param", "-p"],
            description: "Set workflow parameters (format: name=value), accessible as ${{ params.name }}")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var watchOption = new Option<bool>(
            aliases: ["--watch", "-w"],
            description: "Run in watch mode, re-executing on file changes");

        var debounceOption = new Option<int?>(
            aliases: ["--debounce"],
            description: "Debounce interval in milliseconds for watch mode (default: 500)");

        var watchPathOption = new Option<string[]>(
            aliases: ["--watch-path"],
            description: "Glob patterns to watch (overrides YAML config)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var rootCommand = new RootCommand("Workflow Engine - Execute YAML-defined task workflows")
        {
            workflowArgument,
            verboseOption,
            dryRunOption,
            quietOption,
            timeoutOption,
            workingDirOption,
            envOption,
            stepOption,
            noCommandsOption,
            paramOption,
            watchOption,
            debounceOption,
            watchPathOption
        };

        rootCommand.SetHandler(async (context) =>
        {
            var workflow = context.ParseResult.GetValueForArgument(workflowArgument);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var timeout = context.ParseResult.GetValueForOption(timeoutOption);
            var workingDir = context.ParseResult.GetValueForOption(workingDirOption);
            var envVars = context.ParseResult.GetValueForOption(envOption) ?? [];
            var stepMode = context.ParseResult.GetValueForOption(stepOption);
            var noCommands = context.ParseResult.GetValueForOption(noCommandsOption);
            var parameters = context.ParseResult.GetValueForOption(paramOption) ?? [];
            var watchMode = context.ParseResult.GetValueForOption(watchOption);
            var debounce = context.ParseResult.GetValueForOption(debounceOption);
            var watchPaths = context.ParseResult.GetValueForOption(watchPathOption) ?? [];

            var exitCode = await RunWorkflowAsync(new RunOptions
            {
                WorkflowFile = workflow,
                Verbose = verbose,
                DryRun = dryRun,
                Quiet = quiet,
                StepMode = stepMode,
                ShowCommands = !noCommands && !quiet,
                TimeoutSeconds = timeout,
                WorkingDirectory = workingDir,
                EnvironmentVariables = ParseKeyValuePairs(envVars),
                Parameters = ParseKeyValuePairs(parameters),
                WatchMode = watchMode,
                DebounceMs = debounce,
                WatchPaths = watchPaths.ToList(),
                CancellationToken = context.GetCancellationToken()
            });

            context.ExitCode = exitCode;
        });

        // Add validate subcommand
        var validateCommand = new Command("validate", "Validate a workflow file without executing")
        {
            workflowArgument
        };

        validateCommand.SetHandler(async (context) =>
        {
            var workflowFile = context.ParseResult.GetValueForArgument(workflowArgument);
            await ValidateWorkflowAsync(workflowFile, context.GetCancellationToken());
        });

        rootCommand.AddCommand(validateCommand);

        // Add graph subcommand
        var graphCommand = new Command("graph", "Visualize workflow as a dependency graph")
        {
            workflowArgument
        };

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format: ascii, dot")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        formatOption.SetDefaultValue("ascii");
        graphCommand.AddOption(formatOption);

        graphCommand.SetHandler(async (context) =>
        {
            var workflowFile = context.ParseResult.GetValueForArgument(workflowArgument);
            var format = context.ParseResult.GetValueForOption(formatOption) ?? "ascii";
            context.ExitCode = await RenderGraphAsync(workflowFile, format, context.GetCancellationToken());
        });

        rootCommand.AddCommand(graphCommand);

        // Add scheduling commands
        rootCommand.AddCommand(ScheduleCommands.CreateCommand());
        rootCommand.AddCommand(DaemonCommands.CreateCommand());
        rootCommand.AddCommand(DispatchCommand.CreateCommand());

        // Add trigger commands
        rootCommand.AddCommand(TriggerCommands.CreateCommand());

        return rootCommand;
    }

    private static async Task<int> RunWorkflowAsync(RunOptions options)
    {
        if (!options.WorkflowFile.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Workflow file not found: {options.WorkflowFile.FullName}");
            return 1;
        }

        await using var services = BuildServices(options.Verbose);
        var parser = services.GetRequiredService<IWorkflowParser>();
        var runner = services.GetRequiredService<IWorkflowRunner>();
        var scheduler = services.GetRequiredService<IExecutionScheduler>();
        var taskRetrier = services.GetRequiredService<ITaskRetrier>();

        try
        {
            var workflow = parser.ParseFile(options.WorkflowFile.FullName);

            if (!options.Quiet)
            {
                AnsiConsole.MarkupLine($"[bold blue]Workflow:[/] {Markup.Escape(workflow.Name)}");
                if (workflow.Description is not null)
                    AnsiConsole.MarkupLine($"[grey]{Markup.Escape(workflow.Description)}[/]");
                AnsiConsole.MarkupLine($"[grey]Tasks: {workflow.Tasks.Count}[/]");
                AnsiConsole.WriteLine();
            }

            if (options.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]Dry run mode - workflow validated successfully[/]");
                PrintExecutionPlan(workflow, scheduler);
                return 0;
            }

            // Check if watch mode is enabled
            if (options.WatchMode)
            {
                return await RunWatchModeAsync(options, workflow, runner, scheduler, taskRetrier);
            }

            // Build execution plan first
            var plan = scheduler.BuildExecutionPlan(workflow);

            using var renderer = new WorkflowProgressRenderer();
            renderer.SetExecutionPlan(plan, workflow);  // Pass workflow for dependency info
            renderer.SetStepMode(options.StepMode);
            renderer.SetTaskRetrier(taskRetrier);
            runner.OnWorkflowEvent += renderer.OnWorkflowEvent;
            runner.OnTaskEvent += renderer.OnTaskEvent;

            var runOptions = new WorkflowRunOptions
            {
                DryRun = options.DryRun,
                StepMode = options.StepMode,
                StepController = options.StepMode ? renderer : null,
                AdditionalEnvironment = options.EnvironmentVariables,
                OnContextCreated = renderer.SetWorkflowContext,
                ShowCommands = options.ShowCommands,
                Parameters = options.Parameters
            };

            WorkflowContext? context = null;

            if (options.Quiet)
            {
                context = await runner.RunAsync(workflow, runOptions, options.CancellationToken);
                PrintQuietSummary(context);
            }
            else
            {
                await AnsiConsole.Live(renderer.BuildDisplay())
                    .AutoClear(false)
                    .Overflow(VerticalOverflow.Crop)
                    .StartAsync(async ctx =>
                    {
                        renderer.SetLiveContext(ctx);

                        context = await runner.RunAsync(workflow, runOptions, options.CancellationToken);

                        // Keep running for interactive browsing until user presses 'q'
                        await renderer.WaitForExitAsync();
                    });

                // Clear any trailing blank lines left by Live display
                System.Console.Write("\x1b[J");
            }

            return context?.OverallStatus == ExecutionStatus.Succeeded ? 0 : 1;
        }
        catch (WorkflowParsingException ex)
        {
            AnsiConsole.MarkupLine("[red]Workflow validation failed:[/]");
            foreach (var error in ex.Errors)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(error.Message)}");
            }
            return 1;
        }
        catch (CircularDependencyException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Workflow was cancelled[/]");
            return 130; // Standard exit code for SIGINT
        }
        catch (Exception ex)
        {
            if (options.Verbose)
            {
                AnsiConsole.WriteException(ex);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
            return 1;
        }
    }

    private static async Task<int> RunWatchModeAsync(
        RunOptions options,
        Workflow workflow,
        IWorkflowRunner runner,
        IExecutionScheduler scheduler,
        ITaskRetrier taskRetrier)
    {
        // Determine watch configuration
        var watchConfig = workflow.Watch;
        var basePath = options.WorkingDirectory?.FullName ?? Path.GetDirectoryName(options.WorkflowFile.FullName) ?? Directory.GetCurrentDirectory();

        // CLI options override YAML config
        var paths = options.WatchPaths.Count > 0 ? options.WatchPaths : (watchConfig?.Paths.ToList() ?? ["**/*"]);
        var ignore = watchConfig?.Ignore.ToList() ?? [];
        var debounce = options.DebounceMs.HasValue
            ? TimeSpan.FromMilliseconds(options.DebounceMs.Value)
            : (watchConfig?.Debounce ?? TimeSpan.FromMilliseconds(500));

        AnsiConsole.MarkupLine($"[bold cyan]Watch mode enabled[/]");
        AnsiConsole.MarkupLine($"[grey]Base path: {Markup.Escape(basePath)}[/]");
        AnsiConsole.MarkupLine($"[grey]Patterns: {Markup.Escape(string.Join(", ", paths))}[/]");
        if (ignore.Count > 0)
            AnsiConsole.MarkupLine($"[grey]Ignoring: {Markup.Escape(string.Join(", ", ignore))}[/]");
        AnsiConsole.MarkupLine($"[grey]Debounce: {debounce.TotalMilliseconds}ms[/]");
        AnsiConsole.MarkupLine("[grey]Press Ctrl+C to stop[/]");
        AnsiConsole.WriteLine();

        var matcher = new WorkflowEngine.Triggers.FileWatching.GlobMatcher(basePath, paths, ignore);
        var executionCount = 0;
        var lastExitCode = 0;

        using var debouncer = new WorkflowEngine.Triggers.FileWatching.FileChangeDebouncer(debounce, async changes =>
        {
            executionCount++;
            AnsiConsole.MarkupLine($"\n[cyan]Run #{executionCount}[/] - {changes.Count} file(s) changed:");
            foreach (var change in changes.Take(5))
            {
                AnsiConsole.MarkupLine($"  [grey]{change.ChangeType}: {Markup.Escape(change.FileName)}[/]");
            }
            if (changes.Count > 5)
            {
                AnsiConsole.MarkupLine($"  [grey]...and {changes.Count - 5} more[/]");
            }
            AnsiConsole.WriteLine();

            try
            {
                var runOptions = new WorkflowRunOptions
                {
                    DryRun = false,
                    StepMode = false,
                    AdditionalEnvironment = options.EnvironmentVariables,
                    ShowCommands = options.ShowCommands,
                    Parameters = options.Parameters
                };

                var context = await runner.RunAsync(workflow, runOptions, options.CancellationToken);
                lastExitCode = context.OverallStatus == ExecutionStatus.Succeeded ? 0 : 1;

                var statusColor = context.OverallStatus == ExecutionStatus.Succeeded ? "green" : "red";
                var statusText = context.OverallStatus == ExecutionStatus.Succeeded ? "SUCCESS" : "FAILED";
                AnsiConsole.MarkupLine($"[{statusColor}]{statusText}[/] in {context.ElapsedTime.TotalSeconds:F1}s\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}\n");
                lastExitCode = 1;
            }
        });

        using var watcher = new FileSystemWatcher(basePath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        void OnChange(object sender, FileSystemEventArgs e)
        {
            if (matcher.IsMatch(e.FullPath))
            {
                debouncer.FileChanged(e.FullPath, e.ChangeType);
            }
        }

        watcher.Changed += OnChange;
        watcher.Created += OnChange;
        watcher.Renamed += (s, e) =>
        {
            if (matcher.IsMatch(e.FullPath))
            {
                debouncer.FileChanged(e.FullPath, WatcherChangeTypes.Renamed);
            }
        };

        watcher.EnableRaisingEvents = true;

        // Run once on start if configured
        var runOnStart = watchConfig?.RunOnStart ?? true;
        if (runOnStart)
        {
            executionCount++;
            AnsiConsole.MarkupLine($"[cyan]Initial run #{executionCount}[/]");
            AnsiConsole.WriteLine();

            try
            {
                var runOptions = new WorkflowRunOptions
                {
                    DryRun = false,
                    StepMode = false,
                    AdditionalEnvironment = options.EnvironmentVariables,
                    ShowCommands = options.ShowCommands,
                    Parameters = options.Parameters
                };

                var context = await runner.RunAsync(workflow, runOptions, options.CancellationToken);
                lastExitCode = context.OverallStatus == ExecutionStatus.Succeeded ? 0 : 1;

                var statusColor = context.OverallStatus == ExecutionStatus.Succeeded ? "green" : "red";
                var statusText = context.OverallStatus == ExecutionStatus.Succeeded ? "SUCCESS" : "FAILED";
                AnsiConsole.MarkupLine($"[{statusColor}]{statusText}[/] in {context.ElapsedTime.TotalSeconds:F1}s");
                AnsiConsole.MarkupLine("[grey]Watching for changes...[/]\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                AnsiConsole.MarkupLine("[grey]Watching for changes...[/]\n");
                lastExitCode = 1;
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Watching for changes...[/]\n");
        }

        // Wait for cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, options.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("\n[yellow]Watch mode stopped[/]");
        }

        return lastExitCode;
    }

    private static async Task ValidateWorkflowAsync(FileInfo workflowFile, CancellationToken cancellationToken)
    {
        if (!workflowFile.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {workflowFile.FullName}");
            return;
        }

        await using var services = BuildServices(false);
        var parser = services.GetRequiredService<IWorkflowParser>();
        var validator = services.GetRequiredService<IWorkflowValidator>();
        var scheduler = services.GetRequiredService<IExecutionScheduler>();

        try
        {
            var workflow = parser.ParseFile(workflowFile.FullName);
            var result = validator.Validate(workflow);

            if (result.IsValid)
            {
                AnsiConsole.MarkupLine("[green]✓ Workflow is valid[/]");
                PrintExecutionPlan(workflow, scheduler);

                if (result.Warnings.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
                    foreach (var warning in result.Warnings)
                    {
                        AnsiConsole.MarkupLine($"  [yellow]•[/] {Markup.Escape(warning.Message)}");
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]✗ Workflow validation failed:[/]");
                foreach (var error in result.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]•[/] [{error.Code}] {Markup.Escape(error.Message)}");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private static void PrintExecutionPlan(Workflow workflow, IExecutionScheduler scheduler)
    {
        var plan = scheduler.BuildExecutionPlan(workflow);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Execution Plan:[/]");

        foreach (var wave in plan.Waves)
        {
            var taskNames = string.Join(", ", wave.Tasks.Select(t => Markup.Escape(t.DisplayName)));
            AnsiConsole.MarkupLine($"  Wave {wave.WaveIndex}: [[{taskNames}]]");
        }

        if (plan.AlwaysTasks.Count > 0)
        {
            var alwaysNames = string.Join(", ", plan.AlwaysTasks.Select(t => Markup.Escape(t.DisplayName)));
            AnsiConsole.MarkupLine($"  [grey]Always: [[{alwaysNames}]][/]");
        }
    }

    private static async Task<int> RenderGraphAsync(FileInfo workflowFile, string format, CancellationToken cancellationToken)
    {
        if (!workflowFile.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {workflowFile.FullName}");
            return 1;
        }

        await using var services = BuildServices(false);
        var parser = services.GetRequiredService<IWorkflowParser>();
        var scheduler = services.GetRequiredService<IExecutionScheduler>();

        try
        {
            var workflow = parser.ParseFile(workflowFile.FullName);
            var plan = scheduler.BuildExecutionPlan(workflow);

            string output = format.ToLowerInvariant() switch
            {
                "dot" => new DotGraphRenderer().Render(workflow, plan),
                "ascii" or _ => new AsciiGraphRenderer().Render(workflow, plan)
            };

            System.Console.Write(output);
            return 0;
        }
        catch (WorkflowParsingException ex)
        {
            AnsiConsole.MarkupLine("[red]Workflow parsing failed:[/]");
            foreach (var error in ex.Errors)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(error.Message)}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static void PrintQuietSummary(WorkflowContext context)
    {
        var results = context.TaskResults.Values;
        var succeeded = results.Count(r => r.Status == ExecutionStatus.Succeeded);
        var failed = results.Count(r => r.Status == ExecutionStatus.Failed);
        var skipped = results.Count(r => r.Status == ExecutionStatus.Skipped);
        var cancelled = results.Count(r => r.Status == ExecutionStatus.Cancelled);

        var statusColor = Rendering.StatusColorProvider.GetColor(context.OverallStatus);
        var statusText = Rendering.StatusColorProvider.GetLabel(context.OverallStatus);

        var duration = context.ElapsedTime;
        var durationText = duration.TotalSeconds < 1
            ? $"{duration.TotalMilliseconds:F0}ms"
            : duration.TotalMinutes < 1
                ? $"{duration.TotalSeconds:F1}s"
                : $"{duration.TotalMinutes:F1}m";

        AnsiConsole.MarkupLine($"[{statusColor}]{statusText}[/] in {durationText} ({succeeded} passed, {failed} failed, {skipped} skipped)");

        // Show failed task details
        foreach (var result in results.Where(r => r.Status == ExecutionStatus.Failed))
        {
            AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(result.TaskId)}: {Markup.Escape(result.ErrorMessage ?? "Unknown error")}");
        }
    }

    private static Dictionary<string, string> ParseKeyValuePairs(string[] pairs) =>
        pairs
            .Select(e => e.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

    private static ServiceProvider BuildServices(bool verbose) =>
        new ServiceCollection()
            .AddWorkflowEngine(verbose)
            .BuildServiceProvider();

    private sealed record RunOptions
    {
        public required FileInfo WorkflowFile { get; init; }
        public bool Verbose { get; init; }
        public bool DryRun { get; init; }
        public bool Quiet { get; init; }
        public bool StepMode { get; init; }
        public bool ShowCommands { get; init; } = true;
        public int? TimeoutSeconds { get; init; }
        public DirectoryInfo? WorkingDirectory { get; init; }
        public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
        public Dictionary<string, string> Parameters { get; init; } = new();
        public bool WatchMode { get; init; }
        public int? DebounceMs { get; init; }
        public List<string> WatchPaths { get; init; } = [];
        public CancellationToken CancellationToken { get; init; }
    }
}
