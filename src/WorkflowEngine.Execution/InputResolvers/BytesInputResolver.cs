using System.Text;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Execution.InputResolvers;

/// <summary>
/// Resolves base64-encoded or raw bytes task input.
/// </summary>
public sealed class BytesInputResolver : IInputTypeResolver
{
    /// <inheritdoc />
    public InputType SupportedType => InputType.Bytes;

    /// <inheritdoc />
    public Task<byte[]?> ResolveAsync(TaskInput input, WorkflowContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input.Value))
            return Task.FromResult<byte[]?>(null);

        try
        {
            return Task.FromResult<byte[]?>(Convert.FromBase64String(input.Value));
        }
        catch (FormatException)
        {
            // If not valid base64, treat as UTF-8 text
            return Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(input.Value));
        }
    }
}
