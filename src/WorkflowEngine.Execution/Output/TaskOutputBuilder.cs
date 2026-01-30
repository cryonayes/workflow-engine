using System.Text;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.Output;

/// <summary>
/// Builds task output from stdout/stderr streams with size limits and format handling.
/// </summary>
public sealed class TaskOutputBuilder : ITaskOutputBuilder
{
    private const string TruncatedSuffix = "\n...[truncated]";

    private readonly IFileOutputWriter _fileWriter;
    private readonly ILogger<TaskOutputBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskOutputBuilder"/> class.
    /// </summary>
    public TaskOutputBuilder(IFileOutputWriter fileWriter, ILogger<TaskOutputBuilder> logger)
    {
        ArgumentNullException.ThrowIfNull(fileWriter);
        ArgumentNullException.ThrowIfNull(logger);

        _fileWriter = fileWriter;
        _logger = logger;
    }

    /// <inheritdoc />
    public TaskOutput Build(WorkflowTask task, StringBuilder stdOut, StringBuilder stdErr)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(stdOut);
        ArgumentNullException.ThrowIfNull(stdErr);

        var config = task.Output ?? new TaskOutputConfig { MaxSizeBytes = Defaults.MaxOutputSizeBytes };
        var stdOutStr = stdOut.ToString().TrimEnd();
        var stdErrStr = stdErr.ToString().TrimEnd();

        // Enforce max size using byte count (not character count)
        if (config.MaxSizeBytes > 0)
        {
            stdOutStr = TruncateToByteLimit(stdOutStr, config.MaxSizeBytes);
            stdErrStr = TruncateToByteLimit(stdErrStr, config.MaxSizeBytes);
        }

        return config.Type switch
        {
            OutputType.Bytes => new TaskOutput
            {
                RawBytes = Encoding.UTF8.GetBytes(stdOutStr),
                StandardError = config.CaptureStderr ? stdErrStr : null
            },
            OutputType.File when !string.IsNullOrEmpty(config.FilePath) =>
                _fileWriter.WriteToFile(config.FilePath, stdOutStr, stdErrStr, config),
            _ => new TaskOutput
            {
                StandardOutput = stdOutStr,
                StandardError = config.CaptureStderr ? stdErrStr : null
            }
        };
    }

    private static string TruncateToByteLimit(string value, long maxBytes)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount <= maxBytes)
            return value;

        // Binary search for the right character count that fits within byte limit
        var charCount = (int)Math.Min(value.Length, maxBytes);
        while (charCount > 0 && Encoding.UTF8.GetByteCount(value, 0, charCount) > maxBytes - TruncatedSuffix.Length)
            charCount--;

        return value[..charCount] + TruncatedSuffix;
    }
}
