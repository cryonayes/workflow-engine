using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;

namespace WorkflowEngine.Expressions;

/// <summary>
/// Resolves variable references in expressions.
/// </summary>
/// <remarks>
/// Supports the following variable patterns:
/// <list type="bullet">
/// <item><c>tasks.{taskId}.output</c> - Task standard output</item>
/// <item><c>tasks.{taskId}.exitcode</c> - Task exit code</item>
/// <item><c>tasks.{taskId}.status</c> - Task execution status</item>
/// <item><c>tasks.{taskId}.duration</c> - Task duration in milliseconds</item>
/// <item><c>tasks.{taskId}.stderr</c> - Task standard error</item>
/// <item><c>env.{name}</c> - Environment variable</item>
/// <item><c>params.{name}</c> - CLI parameter (--param name=value)</item>
/// <item><c>workflow.name</c> - Workflow name</item>
/// <item><c>workflow.id</c> - Workflow definition ID</item>
/// <item><c>workflow.runid</c> - Current run ID</item>
/// <item><c>fromJson(expr).path</c> - Parse JSON and access property</item>
/// </list>
/// </remarks>
public sealed class VariableInterpolator : IVariableInterpolator
{
    private readonly IJsonFunctions _jsonFunctions;

    /// <summary>
    /// Initializes a new instance with default JSON functions.
    /// </summary>
    public VariableInterpolator() : this(new JsonFunctions())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified JSON functions.
    /// </summary>
    /// <param name="jsonFunctions">The JSON functions provider.</param>
    public VariableInterpolator(IJsonFunctions jsonFunctions)
    {
        _jsonFunctions = jsonFunctions ?? throw new ArgumentNullException(nameof(jsonFunctions));
    }

    /// <summary>
    /// Resolves a variable expression to its value using context.Environment.
    /// </summary>
    /// <param name="expression">The expression to resolve (without ${{ }} wrapper).</param>
    /// <param name="context">The workflow context containing values.</param>
    /// <returns>The resolved value, or empty string if not found.</returns>
    public string Resolve(string expression, WorkflowContext context) =>
        Resolve(expression, context, context.Environment);

    /// <summary>
    /// Resolves a variable expression to its value using a specific environment.
    /// </summary>
    /// <param name="expression">The expression to resolve (without ${{ }} wrapper).</param>
    /// <param name="context">The workflow context containing values.</param>
    /// <param name="environment">The environment dictionary to use for env.* resolution.</param>
    /// <returns>The resolved value, or empty string if not found.</returns>
    public string Resolve(string expression, WorkflowContext context, IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(environment);

        expression = expression.Trim();

        // Handle fromJson() expressions
        if (_jsonFunctions.IsFromJsonExpression(expression))
            return ResolveFromJson(expression, context, environment);

        if (ExpressionPatternMatcher.IsTaskReference(expression))
            return ResolveTaskReference(expression, context);

        if (ExpressionPatternMatcher.IsEnvReference(expression))
            return ResolveEnvironmentVariable(expression, environment);

        if (ExpressionPatternMatcher.IsWorkflowReference(expression))
            return ResolveWorkflowProperty(expression, context);

        if (ExpressionPatternMatcher.IsParamsReference(expression))
            return ResolveParameter(expression, context);

        // Return as-is if not a known pattern
        return expression;
    }

    private string ResolveFromJson(string expression, WorkflowContext context, IReadOnlyDictionary<string, string> environment)
    {
        var parsed = _jsonFunctions.ParseFromJsonExpression(expression);
        if (parsed is null)
            return string.Empty;

        var (innerExpression, propertyPath) = parsed.Value;

        // Resolve the inner expression to get the JSON string
        var jsonString = Resolve(innerExpression, context, environment);
        if (string.IsNullOrEmpty(jsonString))
            return string.Empty;

        // Extract the property from the JSON
        return _jsonFunctions.ExtractJsonProperty(jsonString, propertyPath);
    }

    private static string ResolveTaskReference(string expression, WorkflowContext context)
    {
        var parts = expression.Split('.', 3);
        if (parts.Length < 3)
            return string.Empty;

        var taskId = parts[1];
        var property = parts[2].ToLowerInvariant();

        var result = context.GetTaskResult(taskId);
        if (result is null)
            return string.Empty;

        return property switch
        {
            "output" => result.Output?.AsString() ?? string.Empty,
            "exitcode" => result.ExitCode.ToString(),
            "status" => result.Status.ToString().ToLowerInvariant(),
            "duration" => result.Duration.TotalMilliseconds.ToString("F0"),
            "stderr" => result.Output?.StandardError ?? string.Empty,
            "issuccess" => result.IsSuccess.ToString().ToLowerInvariant(),
            "isfailed" => result.IsFailed.ToString().ToLowerInvariant(),
            "wasskipped" => result.WasSkipped.ToString().ToLowerInvariant(),
            _ => string.Empty
        };
    }

    private static string ResolveEnvironmentVariable(string expression, IReadOnlyDictionary<string, string> environment)
    {
        var varName = ExpressionPatternMatcher.ExtractEnvName(expression) ?? string.Empty;

        // Only use environment variables from the provided dictionary.
        // For local execution, this includes host system vars (merged by WorkflowContextFactory).
        // For Docker execution, this should contain only declared vars to prevent host env leakage.
        // We intentionally do NOT fall back to System.Environment.GetEnvironmentVariable() here
        // as that would bypass the environment isolation for containerized execution.
        return environment.TryGetValue(varName, out var value) ? value : string.Empty;
    }

    private static string ResolveWorkflowProperty(string expression, WorkflowContext context)
    {
        var property = ExpressionPatternMatcher.ExtractWorkflowProperty(expression)?.ToLowerInvariant() ?? string.Empty;

        return property switch
        {
            "name" => context.Workflow.Name,
            "id" => context.Workflow.Id,
            "runid" => context.RunId,
            "workingdirectory" => context.WorkingDirectory,
            "description" => context.Workflow.Description ?? string.Empty,
            "taskcount" => context.Workflow.Tasks.Count.ToString(),
            "elapsedms" => context.ElapsedTime.TotalMilliseconds.ToString("F0"),
            _ => string.Empty
        };
    }

    private static string ResolveParameter(string expression, WorkflowContext context)
    {
        var paramName = ExpressionPatternMatcher.ExtractParamName(expression) ?? string.Empty;
        return context.Parameters.TryGetValue(paramName, out var value) ? value : string.Empty;
    }
}
