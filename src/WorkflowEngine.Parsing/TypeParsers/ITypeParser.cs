namespace WorkflowEngine.Parsing.TypeParsers;

/// <summary>
/// Interface for parsing string values into strongly-typed enums or value objects.
/// Supports Open/Closed principle by allowing new parsers to be added without modifying existing code.
/// </summary>
/// <typeparam name="T">The target type to parse to.</typeparam>
public interface ITypeParser<T>
{
    /// <summary>
    /// Parses a string value into the target type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed value.</returns>
    T Parse(string? value);

    /// <summary>
    /// Tries to parse a string value into the target type.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">The parsed value if successful.</param>
    /// <returns>True if parsing succeeded.</returns>
    bool TryParse(string? value, out T result);
}
