using Microsoft.Extensions.FileSystemGlobbing;

namespace WorkflowEngine.Triggers.FileWatching;

/// <summary>
/// Matches file paths against include and exclude glob patterns.
/// </summary>
public sealed class GlobMatcher
{
    private readonly Matcher _includeMatcher;
    private readonly Matcher _excludeMatcher;
    private readonly string _basePath;
    private readonly bool _hasExcludes;

    /// <summary>
    /// Initializes a new GlobMatcher with the specified patterns.
    /// </summary>
    /// <param name="basePath">The base directory for relative path matching.</param>
    /// <param name="includePatterns">Glob patterns for files to include.</param>
    /// <param name="excludePatterns">Glob patterns for files to exclude.</param>
    public GlobMatcher(string basePath, IEnumerable<string> includePatterns, IEnumerable<string>? excludePatterns = null)
    {
        _basePath = Path.GetFullPath(basePath);

        _includeMatcher = new Matcher();
        foreach (var pattern in includePatterns)
        {
            _includeMatcher.AddInclude(NormalizePattern(pattern));
        }

        _excludeMatcher = new Matcher();
        var excludeList = excludePatterns?.ToList() ?? [];
        _hasExcludes = excludeList.Count > 0;

        foreach (var pattern in excludeList)
        {
            _excludeMatcher.AddInclude(NormalizePattern(pattern));
        }
    }

    /// <summary>
    /// Checks if a file path matches the include patterns and is not excluded.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file matches include patterns and is not excluded.</returns>
    public bool IsMatch(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);
        var relativePath = GetRelativePath(normalizedPath);

        if (string.IsNullOrEmpty(relativePath))
            return false;

        // Check if it matches any include pattern
        var includeResult = _includeMatcher.Match(_basePath, relativePath);
        if (!includeResult.HasMatches)
            return false;

        // Check if it matches any exclude pattern
        if (_hasExcludes)
        {
            var excludeResult = _excludeMatcher.Match(_basePath, relativePath);
            if (excludeResult.HasMatches)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a file path is explicitly excluded.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file matches any exclude pattern.</returns>
    public bool IsIgnored(string filePath)
    {
        if (!_hasExcludes)
            return false;

        var normalizedPath = NormalizePath(filePath);
        var relativePath = GetRelativePath(normalizedPath);

        if (string.IsNullOrEmpty(relativePath))
            return false;

        var excludeResult = _excludeMatcher.Match(_basePath, relativePath);
        return excludeResult.HasMatches;
    }

    /// <summary>
    /// Gets all files in the base directory that match the include patterns.
    /// </summary>
    /// <returns>Enumerable of matching file paths.</returns>
    public IEnumerable<string> GetMatchingFiles()
    {
        var result = _includeMatcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(
            new DirectoryInfo(_basePath)));

        foreach (var file in result.Files)
        {
            var fullPath = Path.Combine(_basePath, file.Path);

            if (_hasExcludes && IsIgnored(fullPath))
                continue;

            yield return fullPath;
        }
    }

    private string GetRelativePath(string fullPath)
    {
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var relative = fullPath[_basePath.Length..];
        return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private static string NormalizePattern(string pattern)
    {
        // Normalize directory separators in patterns
        return pattern.Replace('\\', '/');
    }
}
