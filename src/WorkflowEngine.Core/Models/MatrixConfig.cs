namespace WorkflowEngine.Core.Models;

/// <summary>
/// Configuration for matrix builds that expand a single task into multiple parallel tasks.
/// </summary>
public sealed class MatrixConfig
{
    /// <summary>
    /// Gets the matrix dimensions as key-value pairs where keys are dimension names
    /// and values are lists of possible values for that dimension.
    /// </summary>
    /// <example>
    /// dimensions: { os: [ubuntu, macos], version: [3.10, 3.11] }
    /// </example>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Dimensions { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Gets additional combinations to include in the matrix.
    /// Each dictionary represents a complete set of dimension values to add.
    /// </summary>
    /// <example>
    /// include: [{ os: ubuntu, version: 3.12, experimental: true }]
    /// </example>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Include { get; init; } = [];

    /// <summary>
    /// Gets combinations to exclude from the matrix.
    /// Each dictionary represents a set of dimension values to remove.
    /// </summary>
    /// <example>
    /// exclude: [{ os: windows, version: 3.10 }]
    /// </example>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Exclude { get; init; } = [];

    /// <summary>
    /// Gets whether this matrix configuration has any dimensions defined.
    /// </summary>
    public bool HasDimensions => Dimensions.Count > 0;
}
