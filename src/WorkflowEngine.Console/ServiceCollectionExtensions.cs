using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Execution;
using WorkflowEngine.Execution.InputResolvers;
using WorkflowEngine.Execution.Output;
using WorkflowEngine.Execution.Strategies;
using WorkflowEngine.Expressions;
using WorkflowEngine.Parsing;
using WorkflowEngine.Runner;
using WorkflowEngine.Runner.Events;
using WorkflowEngine.Runner.Execution;
using WorkflowEngine.Runner.Matrix;
using WorkflowEngine.Runner.StepMode;
using WorkflowEngine.Scheduling;
using WorkflowEngine.Scheduling.Abstractions;
using WorkflowEngine.Scheduling.Storage;
using WorkflowEngine.Webhooks;

namespace WorkflowEngine.Console;

/// <summary>
/// Extension methods for configuring WorkflowEngine services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all WorkflowEngine services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowEngine(this IServiceCollection services, bool verbose = false)
    {
        services.AddWorkflowEngineLogging(verbose);
        services.AddWorkflowEngineCore();
        services.AddExpressionServices();
        services.AddExecutionServices();
        services.AddRunnerServices();
        services.AddWebhookServices();

        return services;
    }

    /// <summary>
    /// Configures logging for the workflow engine.
    /// </summary>
    private static IServiceCollection AddWorkflowEngineLogging(this IServiceCollection services, bool verbose)
    {
        if (verbose)
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });
        }
        else
        {
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        }

        return services;
    }

    /// <summary>
    /// Adds core workflow engine services (parsing, validation, scheduling).
    /// </summary>
    private static IServiceCollection AddWorkflowEngineCore(this IServiceCollection services)
    {
        services.AddSingleton<IShellProvider, DefaultShellProvider>();
        services.AddSingleton<IWorkflowValidator, WorkflowValidator>();
        services.AddSingleton<IExecutionScheduler, DagScheduler>();
        services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();

        return services;
    }

    /// <summary>
    /// Adds expression evaluation services (interpolation, conditions, functions).
    /// </summary>
    private static IServiceCollection AddExpressionServices(this IServiceCollection services)
    {
        // Base dependencies
        services.AddSingleton<IVariableInterpolator, VariableInterpolator>();
        services.AddSingleton<IStatusFunctions, StatusFunctions>();
        services.AddSingleton<IStringFunctions>(sp =>
            new StringFunctions(sp.GetRequiredService<IVariableInterpolator>()));

        // ExpressionEvaluator implements multiple interfaces
        services.AddSingleton<ExpressionEvaluator>(sp => new ExpressionEvaluator(
            sp.GetRequiredService<IVariableInterpolator>(),
            sp.GetRequiredService<IStatusFunctions>(),
            sp.GetRequiredService<IStringFunctions>(),
            sp.GetRequiredService<ILogger<ExpressionEvaluator>>()));

        // Register interfaces pointing to the same instance
        services.AddSingleton<IExpressionEvaluator>(sp => sp.GetRequiredService<ExpressionEvaluator>());
        services.AddSingleton<IConditionEvaluator>(sp => sp.GetRequiredService<ExpressionEvaluator>());
        services.AddSingleton<IExpressionInterpolator>(sp => sp.GetRequiredService<ExpressionEvaluator>());

        return services;
    }

    /// <summary>
    /// Adds task execution services (process execution, input resolution, output handling).
    /// </summary>
    private static IServiceCollection AddExecutionServices(this IServiceCollection services)
    {
        // Output handling
        services.AddSingleton<IFileOutputWriter, FileOutputWriter>();
        services.AddSingleton<ITaskOutputBuilder, TaskOutputBuilder>();

        // Input resolver strategies (order matters for resolution priority)
        services.AddSingleton<IInputTypeResolver>(sp =>
            new TextInputResolver(sp.GetRequiredService<IExpressionInterpolator>()));
        services.AddSingleton<IInputTypeResolver, BytesInputResolver>();
        services.AddSingleton<IInputTypeResolver>(sp =>
            new FileInputResolver(sp.GetRequiredService<ILogger<FileInputResolver>>()));
        services.AddSingleton<IInputTypeResolver>(sp =>
            new PipeInputResolver(
                sp.GetRequiredService<IExpressionInterpolator>(),
                sp.GetRequiredService<ILogger<PipeInputResolver>>()));

        // Task input resolver (aggregates all IInputTypeResolver implementations)
        services.AddSingleton<ITaskInputResolver>(sp =>
            new TaskInputResolver(
                sp.GetServices<IInputTypeResolver>(),
                sp.GetRequiredService<ILogger<TaskInputResolver>>()));

        // Docker support
        services.AddSingleton<IDockerCommandBuilder, Execution.Docker.DockerCommandBuilder>();

        // SSH support
        services.AddSingleton<ISshCommandBuilder, Execution.Ssh.SshCommandBuilder>();

        // Execution strategies (order determined by Priority property)
        services.AddSingleton<IExecutionStrategy, SshExecutionStrategy>();
        services.AddSingleton<IExecutionStrategy, DockerExecutionStrategy>();
        services.AddSingleton<IExecutionStrategy, LocalExecutionStrategy>();

        // Process and task execution
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IRetryPolicy, DefaultRetryPolicy>();
        services.AddSingleton<ITaskExecutor, TaskExecutor>();

        return services;
    }

    /// <summary>
    /// Adds workflow runner services (orchestration, events, step mode).
    /// </summary>
    private static IServiceCollection AddRunnerServices(this IServiceCollection services)
    {
        // Matrix expansion components
        services.AddSingleton<IMatrixCombinationGenerator, MatrixCombinationGenerator>();
        services.AddSingleton<IMatrixExpressionInterpolator, MatrixExpressionInterpolator>();
        services.AddSingleton<IExpandedTaskBuilder, ExpandedTaskBuilder>();
        services.AddSingleton<IDependencyRewriter, DependencyRewriter>();
        services.AddSingleton<IMatrixExpander, MatrixExpander>();

        // Runner services
        services.AddSingleton<IEventPublisher, WorkflowEventPublisher>();
        services.AddSingleton<IStepModeHandler, StepModeHandler>();
        services.AddSingleton<IWaveExecutor, WaveExecutor>();
        services.AddSingleton<IWorkflowRunner, WorkflowRunner>();
        services.AddSingleton<ITaskRetrier, TaskRetrier>();

        return services;
    }

    /// <summary>
    /// Adds workflow scheduling services (scheduler, storage, cron parsing).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="storagePath">Optional path to the schedules storage file.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSchedulingServices(this IServiceCollection services, string? storagePath = null)
    {
        // Add logging (minimal by default for daemon)
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));

        // Add core workflow services needed for execution
        services.AddWorkflowEngine(verbose: false);

        // Add scheduling-specific services
        services.AddSingleton<ICronParser, CronParser>();
        services.AddSingleton<IScheduleStorage>(_ => new JsonFileScheduleStorage(storagePath));
        services.AddSingleton<IScheduleRunner, ScheduleRunner>();

        // Register BackgroundScheduler as the main implementation
        services.AddSingleton<BackgroundScheduler>();

        // Register all interfaces pointing to the same instance (ISP compliance)
        services.AddSingleton<IScheduler>(sp => sp.GetRequiredService<BackgroundScheduler>());
        services.AddSingleton<ISchedulerLifecycle>(sp => sp.GetRequiredService<BackgroundScheduler>());
        services.AddSingleton<IScheduleRepository>(sp => sp.GetRequiredService<BackgroundScheduler>());
        services.AddSingleton<IScheduleExecutor>(sp => sp.GetRequiredService<BackgroundScheduler>());
        services.AddSingleton<ISchedulerEvents>(sp => sp.GetRequiredService<BackgroundScheduler>());

        return services;
    }
}
