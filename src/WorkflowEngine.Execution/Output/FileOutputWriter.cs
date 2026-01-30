using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Output;

/// <summary>
/// Writes task output to files with security validation.
/// </summary>
public sealed class FileOutputWriter : IFileOutputWriter
{
    private readonly ILogger<FileOutputWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOutputWriter"/> class.
    /// </summary>
    public FileOutputWriter(ILogger<FileOutputWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public TaskOutput WriteToFile(string filePath, string stdOut, string stdErr, TaskOutputConfig config)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            // Validate path doesn't contain dangerous patterns - security: reject path traversal
            var fullPath = Path.GetFullPath(filePath);
            if (fullPath.Contains("..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Path traversal detected in output file path: {filePath}");
            }

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, stdOut);

            return new TaskOutput
            {
                OutputFilePath = fullPath,
                StandardError = config.CaptureStderr ? stdErr : null
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _logger.LogError(ex, "Failed to write output to file: {FilePath}", filePath);

            // Fall back to in-memory output
            return new TaskOutput
            {
                StandardOutput = stdOut,
                StandardError = config.CaptureStderr ? stdErr : null
            };
        }
    }
}
