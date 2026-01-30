namespace WorkflowEngine.Core.Utilities;

/// <summary>
/// Utility class for matching common expression patterns.
/// Consolidates repeated pattern checks for DRY compliance.
/// </summary>
public static class ExpressionPatternMatcher
{
    /// <summary>
    /// Expression prefix for task references (e.g., "tasks.taskId.output").
    /// </summary>
    public const string TasksPrefix = "tasks.";

    /// <summary>
    /// Expression prefix for environment variables (e.g., "env.VAR_NAME").
    /// </summary>
    public const string EnvPrefix = "env.";

    /// <summary>
    /// Expression prefix for workflow properties (e.g., "workflow.name").
    /// </summary>
    public const string WorkflowPrefix = "workflow.";

    /// <summary>
    /// Expression prefix for matrix variables (e.g., "matrix.os").
    /// </summary>
    public const string MatrixPrefix = "matrix.";

    /// <summary>
    /// Expression prefix for parameters (e.g., "params.name").
    /// </summary>
    public const string ParamsPrefix = "params.";

    /// <summary>
    /// Determines if the expression is a task reference.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression starts with "tasks."</returns>
    public static bool IsTaskReference(string expression) =>
        expression.StartsWith(TasksPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the expression is an environment variable reference.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression starts with "env."</returns>
    public static bool IsEnvReference(string expression) =>
        expression.StartsWith(EnvPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the expression is a workflow property reference.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression starts with "workflow."</returns>
    public static bool IsWorkflowReference(string expression) =>
        expression.StartsWith(WorkflowPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the expression is a matrix variable reference.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression starts with "matrix."</returns>
    public static bool IsMatrixReference(string expression) =>
        expression.StartsWith(MatrixPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the expression is a parameter reference.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression starts with "params."</returns>
    public static bool IsParamsReference(string expression) =>
        expression.StartsWith(ParamsPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the expression is a variable reference (task, env, workflow, matrix, or params).
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression is any type of variable reference.</returns>
    public static bool IsVariableReference(string expression) =>
        IsTaskReference(expression) || IsEnvReference(expression) || IsWorkflowReference(expression) || IsMatrixReference(expression) || IsParamsReference(expression);

    /// <summary>
    /// Extracts the task ID from a task reference expression.
    /// </summary>
    /// <param name="expression">The expression (e.g., "tasks.myTask.output").</param>
    /// <returns>The task ID (e.g., "myTask"), or null if not a valid task reference.</returns>
    public static string? ExtractTaskId(string expression)
    {
        if (!IsTaskReference(expression))
            return null;

        var afterPrefix = expression[TasksPrefix.Length..];
        var dotIndex = afterPrefix.IndexOf('.');
        return dotIndex > 0 ? afterPrefix[..dotIndex] : afterPrefix;
    }

    /// <summary>
    /// Extracts the variable name from an environment reference expression.
    /// </summary>
    /// <param name="expression">The expression (e.g., "env.MY_VAR").</param>
    /// <returns>The variable name (e.g., "MY_VAR"), or null if not a valid env reference.</returns>
    public static string? ExtractEnvName(string expression)
    {
        if (!IsEnvReference(expression))
            return null;

        return expression[EnvPrefix.Length..];
    }

    /// <summary>
    /// Extracts the property name from a workflow reference expression.
    /// </summary>
    /// <param name="expression">The expression (e.g., "workflow.name").</param>
    /// <returns>The property name (e.g., "name"), or null if not a valid workflow reference.</returns>
    public static string? ExtractWorkflowProperty(string expression)
    {
        if (!IsWorkflowReference(expression))
            return null;

        return expression[WorkflowPrefix.Length..];
    }

    /// <summary>
    /// Extracts the key from a matrix reference expression.
    /// </summary>
    /// <param name="expression">The expression (e.g., "matrix.os").</param>
    /// <returns>The matrix key (e.g., "os"), or null if not a valid matrix reference.</returns>
    public static string? ExtractMatrixKey(string expression)
    {
        if (!IsMatrixReference(expression))
            return null;

        return expression[MatrixPrefix.Length..];
    }

    /// <summary>
    /// Extracts the parameter name from a params reference expression.
    /// </summary>
    /// <param name="expression">The expression (e.g., "params.version").</param>
    /// <returns>The parameter name (e.g., "version"), or null if not a valid params reference.</returns>
    public static string? ExtractParamName(string expression)
    {
        if (!IsParamsReference(expression))
            return null;

        return expression[ParamsPrefix.Length..];
    }
}
