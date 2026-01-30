using System.Text.RegularExpressions;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Expressions;

/// <summary>
/// String manipulation functions for expressions.
/// </summary>
public sealed partial class StringFunctions : IStringFunctions
{
    private readonly IVariableInterpolator _interpolator;

    [GeneratedRegex(@"^(\w+)\s*\(\s*(.+?)\s*,\s*(.+?)\s*\)$")]
    private static partial Regex TwoArgFunctionPattern();

    [GeneratedRegex(@"^(\w+)\s*\(\s*(.+?)\s*\)$")]
    private static partial Regex OneArgFunctionPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="StringFunctions"/> class.
    /// </summary>
    /// <param name="interpolator">The variable interpolator for resolving expressions.</param>
    public StringFunctions(IVariableInterpolator interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        _interpolator = interpolator;
    }

    private static readonly HashSet<string> TwoArgFunctions = ["contains", "startswith", "endswith", "equals"];
    private static readonly HashSet<string> OneArgFunctions = ["isempty", "isnotempty"];

    /// <summary>
    /// Tries to evaluate a string function.
    /// </summary>
    public bool TryEvaluate(string expression, WorkflowContext context, out bool result)
    {
        result = false;

        // Two-arg functions: contains(x, y), startsWith(x, y), endsWith(x, y), equals(x, y)
        var twoArgMatch = TwoArgFunctionPattern().Match(expression.Trim());
        if (twoArgMatch.Success)
        {
            var funcName = twoArgMatch.Groups[1].Value.ToLowerInvariant();
            if (!TwoArgFunctions.Contains(funcName)) return false;

            var arg1 = ResolveArgument(twoArgMatch.Groups[2].Value, context);
            var arg2 = ResolveArgument(twoArgMatch.Groups[3].Value, context);

            result = funcName switch
            {
                "contains" => arg1.Contains(arg2, StringComparison.OrdinalIgnoreCase),
                "startswith" => arg1.StartsWith(arg2, StringComparison.OrdinalIgnoreCase),
                "endswith" => arg1.EndsWith(arg2, StringComparison.OrdinalIgnoreCase),
                "equals" => string.Equals(arg1, arg2, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
            return true;
        }

        // One-arg functions: isEmpty(x), isNotEmpty(x)
        var oneArgMatch = OneArgFunctionPattern().Match(expression.Trim());
        if (oneArgMatch.Success)
        {
            var funcName = oneArgMatch.Groups[1].Value.ToLowerInvariant();
            if (!OneArgFunctions.Contains(funcName)) return false;

            var arg = ResolveArgument(oneArgMatch.Groups[2].Value, context);
            result = funcName == "isempty" ? string.IsNullOrEmpty(arg) : !string.IsNullOrEmpty(arg);
            return true;
        }

        return false;
    }

    private string ResolveArgument(string arg, WorkflowContext context)
    {
        arg = arg.Trim();

        // Remove quotes if it's a string literal
        if ((arg.StartsWith('\'') && arg.EndsWith('\'')) ||
            (arg.StartsWith('"') && arg.EndsWith('"')))
        {
            return arg[1..^1];
        }

        return _interpolator.Resolve(arg, context);
    }
}
