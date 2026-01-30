namespace WorkflowEngine.Expressions;

/// <summary>
/// Interface for JSON parsing functions in expressions.
/// </summary>
public interface IJsonFunctions
{
    /// <summary>
    /// Determines if an expression is a fromJson() call.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression starts with fromJson(.</returns>
    bool IsFromJsonExpression(string expression);

    /// <summary>
    /// Parses a fromJson() expression into its components.
    /// </summary>
    /// <param name="expression">The expression like "fromJson(tasks.api.output).user.id".</param>
    /// <returns>
    /// A tuple of (innerExpression, propertyPath) or null if not a valid fromJson expression.
    /// For "fromJson(tasks.api.output).user.id", returns ("tasks.api.output", "user.id").
    /// </returns>
    (string innerExpression, string propertyPath)? ParseFromJsonExpression(string expression);

    /// <summary>
    /// Extracts a value from JSON using a property path.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="propertyPath">The dot-notation path to the property (e.g., "user.name" or "items[0].id").</param>
    /// <returns>The extracted value as a string, or empty string if not found or invalid.</returns>
    string ExtractJsonProperty(string json, string propertyPath);
}
