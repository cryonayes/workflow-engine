using FluentAssertions;
using WorkflowEngine.Triggers.FileWatching;

namespace WorkflowEngine.Tests.Triggers.FileWatching;

public class GlobMatcherTests : IDisposable
{
    private readonly string _tempDir;

    public GlobMatcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"glob-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsMatch_MatchesSimplePattern()
    {
        CreateFile("test.cs");
        var matcher = new GlobMatcher(_tempDir, ["*.cs"]);

        matcher.IsMatch(Path.Combine(_tempDir, "test.cs")).Should().BeTrue();
        matcher.IsMatch(Path.Combine(_tempDir, "test.txt")).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_MatchesRecursivePattern()
    {
        CreateFile("src/Controllers/UserController.cs");
        CreateFile("src/Models/User.cs");
        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"]);

        matcher.IsMatch(Path.Combine(_tempDir, "src/Controllers/UserController.cs")).Should().BeTrue();
        matcher.IsMatch(Path.Combine(_tempDir, "src/Models/User.cs")).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_RespectsExcludePatterns()
    {
        CreateFile("src/main.cs");
        CreateFile("src/bin/output.cs");
        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"], ["**/bin/**"]);

        matcher.IsMatch(Path.Combine(_tempDir, "src/main.cs")).Should().BeTrue();
        matcher.IsMatch(Path.Combine(_tempDir, "src/bin/output.cs")).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_MultipleIncludePatterns()
    {
        CreateFile("app.ts");
        CreateFile("app.tsx");
        CreateFile("app.js");
        var matcher = new GlobMatcher(_tempDir, ["*.ts", "*.tsx"]);

        matcher.IsMatch(Path.Combine(_tempDir, "app.ts")).Should().BeTrue();
        matcher.IsMatch(Path.Combine(_tempDir, "app.tsx")).Should().BeTrue();
        matcher.IsMatch(Path.Combine(_tempDir, "app.js")).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_MultipleExcludePatterns()
    {
        CreateFile("src/code.cs");
        CreateFile("bin/output.cs");
        CreateFile("obj/cache.cs");
        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"], ["**/bin/**", "**/obj/**"]);

        matcher.IsMatch(Path.Combine(_tempDir, "src/code.cs")).Should().BeTrue();
        matcher.IsMatch(Path.Combine(_tempDir, "bin/output.cs")).Should().BeFalse();
        matcher.IsMatch(Path.Combine(_tempDir, "obj/cache.cs")).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_ReturnsTrue_ForExcludedFiles()
    {
        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"], ["**/bin/**"]);

        matcher.IsIgnored(Path.Combine(_tempDir, "bin/output.cs")).Should().BeTrue();
        matcher.IsIgnored(Path.Combine(_tempDir, "src/main.cs")).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_ReturnsFalse_WhenNoExcludePatterns()
    {
        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"]);

        matcher.IsIgnored(Path.Combine(_tempDir, "any/file.cs")).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_ReturnsFalse_ForPathsOutsideBaseDir()
    {
        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"]);
        var outsidePath = Path.Combine(Path.GetTempPath(), "outside.cs");

        matcher.IsMatch(outsidePath).Should().BeFalse();
    }

    [Fact]
    public void GetMatchingFiles_ReturnsMatchingFilesInDirectory()
    {
        CreateFile("src/main.cs");
        CreateFile("src/lib.cs");
        CreateFile("src/readme.md");
        CreateFile("bin/output.dll");

        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"]);

        var matches = matcher.GetMatchingFiles().ToList();

        matches.Should().HaveCount(2);
        matches.Should().Contain(Path.Combine(_tempDir, "src/main.cs"));
        matches.Should().Contain(Path.Combine(_tempDir, "src/lib.cs"));
    }

    [Fact]
    public void GetMatchingFiles_RespectsExcludePatterns()
    {
        CreateFile("src/main.cs");
        CreateFile("bin/output.cs");

        var matcher = new GlobMatcher(_tempDir, ["**/*.cs"], ["**/bin/**"]);

        var matches = matcher.GetMatchingFiles().ToList();

        matches.Should().ContainSingle()
            .Which.Should().EndWith("main.cs");
    }

    private void CreateFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, "test content");
    }
}
