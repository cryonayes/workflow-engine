using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Core;
using WorkflowEngine.Core.Abstractions;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Utilities;
using WorkflowEngine.Expressions.Functions;

namespace WorkflowEngine.Expressions;

/// <summary>
/// Evaluates expressions for conditions and variable interpolation in workflows.
/// </summary>
/// <remarks>
/// Supports GitHub Actions-style expressions:
/// <list type="bullet">
/// <item><c>${{ expression }}</c> - expression wrapper</item>
/// <item><c>tasks.taskId.output</c> - reference task output</item>
/// <item><c>env.VAR_NAME</c> - reference environment variable</item>
/// <item><c>success()</c>, <c>failure()</c>, <c>always()</c>, <c>cancelled()</c> - status functions</item>
/// <item><c>==</c>, <c>!=</c>, <c>&gt;</c>, <c>&lt;</c>, <c>&gt;=</c>, <c>&lt;=</c> - comparisons</item>
/// <item><c>&amp;&amp;</c>, <c>||</c>, <c>!</c> - boolean operators</item>
/// </list>
/// </remarks>
public sealed partial class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly IVariableInterpolator _interpolator;
    private readonly IStatusFunctions _statusFunctions;
    private readonly IStringFunctions _stringFunctions;
    private readonly IFunctionRegistry _functionRegistry;
    private readonly ILogger<ExpressionEvaluator> _logger;

    [GeneratedRegex(@"\$\{\{\s*(.+?)\s*\}\}")]
    private static partial Regex ExpressionPatternRegex();

    [GeneratedRegex(@"^(.+?)\s*(==|!=|>=|<=|>|<)\s*(.+)$")]
    private static partial Regex ComparisonPatternRegex();

    [GeneratedRegex(@"^(\w+)\(\)$")]
    private static partial Regex FunctionCallPatternRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
    /// </summary>
    public ExpressionEvaluator() : this(NullLogger<ExpressionEvaluator>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance with a logger.
    /// </summary>
    public ExpressionEvaluator(ILogger<ExpressionEvaluator> logger)
        : this(
            new VariableInterpolator(),
            new StatusFunctions(),
            new StringFunctions(new VariableInterpolator()),
            FunctionRegistry.CreateDefault(),
            logger)
    {
    }

    /// <summary>
    /// Initializes a new instance with all dependencies injected.
    /// </summary>
    public ExpressionEvaluator(
        IVariableInterpolator interpolator,
        IStatusFunctions statusFunctions,
        IStringFunctions stringFunctions,
        ILogger<ExpressionEvaluator> logger)
        : this(interpolator, statusFunctions, stringFunctions, FunctionRegistry.CreateDefault(), logger)
    {
    }

    /// <summary>
    /// Initializes a new instance with all dependencies including function registry.
    /// </summary>
    public ExpressionEvaluator(
        IVariableInterpolator interpolator,
        IStatusFunctions statusFunctions,
        IStringFunctions stringFunctions,
        IFunctionRegistry functionRegistry,
        ILogger<ExpressionEvaluator> logger)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        ArgumentNullException.ThrowIfNull(statusFunctions);
        ArgumentNullException.ThrowIfNull(stringFunctions);
        ArgumentNullException.ThrowIfNull(functionRegistry);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _interpolator = interpolator;
        _statusFunctions = statusFunctions;
        _stringFunctions = stringFunctions;
        _functionRegistry = functionRegistry;
    }

    /// <inheritdoc />
    public bool EvaluateCondition(string expression, WorkflowContext context) =>
        EvaluateCondition(expression, context, null);

    /// <inheritdoc />
    public bool EvaluateCondition(string expression, WorkflowContext context, IEnumerable<string>? dependsOn)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(context);

        var inner = ExtractExpression(expression);

        _logger.LogDebug("Evaluating condition: {Expression}", inner);

        try
        {
            var result = EvaluateInner(inner, context, dependsOn);
            _logger.LogDebug("Condition '{Expression}' evaluated to {Result}", inner, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate condition '{Expression}', defaulting to false", inner);
            return false;
        }
    }

    private bool EvaluateInner(string inner, WorkflowContext context, IEnumerable<string>? dependsOn)
    {
        // Status functions
        if (TryEvaluateStatusFunction(inner, context, dependsOn, out var statusResult))
            return statusResult;

        // Boolean operators (check before string functions)
        if (inner.Contains("&&"))
            return inner.Split("&&", StringSplitOptions.TrimEntries).All(p => EvaluateInner(p, context, dependsOn));

        if (inner.Contains("||"))
            return inner.Split("||", StringSplitOptions.TrimEntries).Any(p => EvaluateInner(p, context, dependsOn));

        // String functions
        if (_stringFunctions.TryEvaluate(inner, context, out var stringResult))
            return stringResult;

        // Negation
        if (inner.StartsWith('!'))
        {
            var negated = inner[1..].Trim();
            // Handle !(expression) wrapping - only unwrap if starts with ( and no function call inside
            if (negated.StartsWith('(') && negated.EndsWith(')'))
            {
                var inside = negated[1..^1];
                // Don't unwrap if it's a function call like (success())
                if (!inside.EndsWith(')'))
                    negated = inside;
            }
            return !EvaluateInner(negated, context, dependsOn);
        }

        return EvaluateBooleanExpression(inner, context);
    }

    private bool TryEvaluateStatusFunction(string inner, WorkflowContext context, IEnumerable<string>? dependsOn, out bool result)
    {
        result = false;

        // Try to match function call pattern like "success()" or "failure()"
        var match = FunctionCallPatternRegex().Match(inner.ToLowerInvariant());
        if (!match.Success)
            return false;

        var functionName = match.Groups[1].Value;
        var function = _functionRegistry.Get(functionName);

        if (function is null)
            return false;

        var dependsList = dependsOn?.ToList() ?? [];
        result = function.Evaluate(context, dependsList);
        return true;
    }

    /// <inheritdoc />
    public string Interpolate(string template, WorkflowContext context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        ArgumentNullException.ThrowIfNull(context);

        return ExpressionPatternRegex().Replace(template, match =>
        {
            var expression = match.Groups[1].Value.Trim();
            return _interpolator.Resolve(expression, context);
        });
    }

    /// <inheritdoc />
    public string Interpolate(string template, WorkflowContext context, IReadOnlyDictionary<string, string> environment)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(environment);

        return ExpressionPatternRegex().Replace(template, match =>
        {
            var expression = match.Groups[1].Value.Trim();
            return _interpolator.Resolve(expression, context, environment);
        });
    }

    private static string ExtractExpression(string expression)
    {
        var match = ExpressionPatternRegex().Match(expression);
        return match.Success ? match.Groups[1].Value.Trim() : expression.Trim();
    }

    /// <summary>
    /// Evaluates a boolean expression after variable resolution.
    /// </summary>
    /// <remarks>
    /// Truthiness rules (following JavaScript-like semantics):
    /// - Falsy values: null, empty string, whitespace-only, "0", "false" (case-insensitive)
    /// - Truthy values: any non-empty string that isn't "0" or "false"
    /// </remarks>
    private bool EvaluateBooleanExpression(string expression, WorkflowContext context)
    {
        expression = expression.Trim();

        // Boolean literals
        if (bool.TryParse(expression, out var boolResult))
            return boolResult;

        // Comparison expressions
        if (TryEvaluateComparison(expression, context, out var comparisonResult))
            return comparisonResult;

        // Resolve variable if it's a simple reference
        var resolved = _interpolator.Resolve(expression, context);

        // Truthiness evaluation
        return IsTruthy(resolved);
    }

    private static bool IsTruthy(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value != "0" &&
        !value.Equals("false", StringComparison.OrdinalIgnoreCase);

    private bool TryEvaluateComparison(string expression, WorkflowContext context, out bool result)
    {
        result = false;

        var match = ComparisonPatternRegex().Match(expression);
        if (!match.Success)
            return false;

        var leftRaw = match.Groups[1].Value.Trim();
        var op = match.Groups[2].Value;
        var rightRaw = match.Groups[3].Value.Trim();

        // Resolve variables in left and right operands
        var left = ResolveOperand(leftRaw, context);
        var right = ResolveOperand(rightRaw, context);

        // Numeric comparison
        if (double.TryParse(left, out var leftNum) && double.TryParse(right, out var rightNum))
        {
            result = op switch
            {
                "==" => Math.Abs(leftNum - rightNum) < Precision.FloatEpsilon,
                "!=" => Math.Abs(leftNum - rightNum) >= Precision.FloatEpsilon,
                ">" => leftNum > rightNum,
                "<" => leftNum < rightNum,
                ">=" => leftNum >= rightNum,
                "<=" => leftNum <= rightNum,
                _ => false
            };
            return true;
        }

        // String comparison
        result = op switch
        {
            "==" => string.Equals(left, right, StringComparison.Ordinal),
            "!=" => !string.Equals(left, right, StringComparison.Ordinal),
            _ => false
        };
        return true;
    }

    private string ResolveOperand(string operand, WorkflowContext context)
    {
        // Remove quotes if present
        operand = operand.Trim('\'', '"');

        // Check if it's a variable reference (starts with tasks., env., or workflow.)
        return ExpressionPatternMatcher.IsVariableReference(operand)
            ? _interpolator.Resolve(operand, context)
            : operand;
    }
}
