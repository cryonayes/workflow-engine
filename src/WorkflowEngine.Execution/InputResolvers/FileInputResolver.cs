using Microsoft.Extensions.Logging;
using WorkflowEngine.Core;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.InputResolvers;

/// <summary>
/// Resolves file-based task input.
/// </summary>
public sealed class FileInputResolver : IInputTypeResolver
{
    private readonly ILogger<FileInputResolver> _logger;

    /// <inheritdoc />
    public InputType SupportedType => InputType.File;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileInputResolver"/> class.
    /// </summary>
    public FileInputResolver(ILogger<FileInputResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]?> ResolveAsync(TaskInput input, WorkflowContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input.FilePath))
        {
            _logger.LogWarning("File input specified but no file path provided");
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(input.FilePath);

            if (!fileInfo.Exists)
            {
                _logger.LogWarning("Input file not found: {FilePath}", input.FilePath);
                return null;
            }

            // Validate file size to prevent loading huge files into memory
            if (fileInfo.Length > Defaults.MaxOutputSizeBytes)
            {
                _logger.LogError(
                    "Input file exceeds maximum size limit ({FileSize} > {MaxSize}): {FilePath}",
                    fileInfo.Length, Defaults.MaxOutputSizeBytes, input.FilePath);
                throw new InvalidOperationException(
                    $"Input file '{input.FilePath}' exceeds maximum allowed size of {Defaults.MaxOutputSizeBytes / (1024 * 1024)}MB");
            }

            return await File.ReadAllBytesAsync(input.FilePath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to read input file: {FilePath}", input.FilePath);
            throw;
        }
    }
}
