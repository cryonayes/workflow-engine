using System.Text;

namespace WorkflowEngine.Core.Models;

/// <summary>
/// Contains the captured output from a task execution.
/// </summary>
public sealed class TaskOutput
{
    /// <summary>
    /// Standard output as a string.
    /// </summary>
    public string? StandardOutput { get; init; }

    /// <summary>
    /// Gets the standard error output as a string.
    /// </summary>
    public string? StandardError { get; init; }

    /// <summary>
    /// Raw output bytes for binary data.
    /// </summary>
    public byte[]? RawBytes { get; init; }

    /// <summary>
    /// Path to the output file when written to disk.
    /// </summary>
    public string? OutputFilePath { get; init; }

    /// <summary>
    /// Gets the output as a string, regardless of how it was captured.
    /// </summary>
    /// <returns>The output content as a UTF-8 string, or empty string if no output.</returns>
    /// <exception cref="IOException">Thrown if the output file cannot be read.</exception>
    public string AsString()
    {
        if (StandardOutput is not null)
            return StandardOutput;

        if (RawBytes is not null)
            return Encoding.UTF8.GetString(RawBytes);

        if (OutputFilePath is not null && File.Exists(OutputFilePath))
            return File.ReadAllText(OutputFilePath);

        return string.Empty;
    }

    /// <summary>
    /// Gets the output as bytes, regardless of how it was captured.
    /// </summary>
    /// <returns>The output content as bytes, or empty array if no output.</returns>
    /// <exception cref="IOException">Thrown if the output file cannot be read.</exception>
    public byte[] AsBytes()
    {
        if (RawBytes is not null)
            return RawBytes;

        if (StandardOutput is not null)
            return Encoding.UTF8.GetBytes(StandardOutput);

        if (OutputFilePath is not null && File.Exists(OutputFilePath))
            return File.ReadAllBytes(OutputFilePath);

        return Array.Empty<byte>();
    }

    /// <summary>
    /// Gets whether this output contains any data.
    /// </summary>
    public bool HasContent =>
        !string.IsNullOrEmpty(StandardOutput) ||
        (RawBytes is not null && RawBytes.Length > 0) ||
        (!string.IsNullOrEmpty(OutputFilePath) && File.Exists(OutputFilePath));

    /// <summary>
    /// Gets whether there was any error output.
    /// </summary>
    public bool HasErrors => !string.IsNullOrEmpty(StandardError);

    /// <summary>
    /// Creates an empty output instance.
    /// </summary>
    public static TaskOutput Empty { get; } = new();
}
