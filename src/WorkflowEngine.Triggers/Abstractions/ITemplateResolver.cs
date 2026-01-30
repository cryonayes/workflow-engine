using WorkflowEngine.Triggers.Models;

namespace WorkflowEngine.Triggers.Abstractions;

/// <summary>
/// Resolves template placeholders in strings.
/// </summary>
public interface ITemplateResolver
{
    /// <summary>
    /// Resolves placeholders in a template string.
    /// </summary>
    /// <param name="template">The template with placeholders.</param>
    /// <param name="captures">Captured values from pattern matching.</param>
    /// <param name="message">The incoming message for context.</param>
    /// <param name="additionalValues">Additional values to substitute.</param>
    /// <returns>The resolved string.</returns>
    string Resolve(
        string template,
        IReadOnlyDictionary<string, string> captures,
        IncomingMessage message,
        IReadOnlyDictionary<string, string>? additionalValues = null);

    /// <summary>
    /// Resolves placeholders in a dictionary of parameters.
    /// </summary>
    /// <param name="parameters">The parameters with placeholder values.</param>
    /// <param name="captures">Captured values from pattern matching.</param>
    /// <param name="message">The incoming message for context.</param>
    /// <returns>Dictionary with resolved values.</returns>
    IReadOnlyDictionary<string, string> ResolveParameters(
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<string, string> captures,
        IncomingMessage message);
}
