using FluentAssertions;
using WorkflowEngine.Triggers.FileWatching;

namespace WorkflowEngine.Tests.Triggers.FileWatching;

public class FileChangeDebouncerTests : IDisposable
{
    private readonly List<IReadOnlyList<FileChangeInfo>> _receivedChanges = [];
    private FileChangeDebouncer? _debouncer;

    public void Dispose()
    {
        _debouncer?.Dispose();
    }

    [Fact]
    public async Task FileChanged_DebouncesSingleChange()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromMilliseconds(50),
            changes => _receivedChanges.Add(changes));

        _debouncer.FileChanged("/path/to/file.cs");

        // Should not trigger immediately
        _receivedChanges.Should().BeEmpty();

        // Wait for debounce
        await Task.Delay(100);

        _receivedChanges.Should().ContainSingle();
        _receivedChanges[0].Should().ContainSingle()
            .Which.FilePath.Should().Be("/path/to/file.cs");
    }

    [Fact]
    public async Task FileChanged_ConsolidatesRapidChanges()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromMilliseconds(100),
            changes => _receivedChanges.Add(changes));

        _debouncer.FileChanged("/path/file1.cs");
        await Task.Delay(20);
        _debouncer.FileChanged("/path/file2.cs");
        await Task.Delay(20);
        _debouncer.FileChanged("/path/file3.cs");

        // Should not trigger yet
        _receivedChanges.Should().BeEmpty();

        // Wait for debounce
        await Task.Delay(150);

        _receivedChanges.Should().ContainSingle();
        _receivedChanges[0].Should().HaveCount(3);
    }

    [Fact]
    public async Task FileChanged_LastChangeWinsForSameFile()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromMilliseconds(50),
            changes => _receivedChanges.Add(changes));

        _debouncer.FileChanged("/path/file.cs", WatcherChangeTypes.Created);
        await Task.Delay(10);
        _debouncer.FileChanged("/path/file.cs", WatcherChangeTypes.Changed);
        await Task.Delay(10);
        _debouncer.FileChanged("/path/file.cs", WatcherChangeTypes.Changed);

        await Task.Delay(100);

        _receivedChanges.Should().ContainSingle();
        _receivedChanges[0].Should().ContainSingle()
            .Which.ChangeType.Should().Be(WatcherChangeTypes.Changed);
    }

    [Fact]
    public void Flush_TriggersImmediately()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromSeconds(10), // Long debounce
            changes => _receivedChanges.Add(changes));

        _debouncer.FileChanged("/path/file.cs");
        _debouncer.Flush();

        _receivedChanges.Should().ContainSingle();
    }

    [Fact]
    public void Flush_ClearsTimer()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromMilliseconds(50),
            changes => _receivedChanges.Add(changes));

        _debouncer.FileChanged("/path/file.cs");
        _debouncer.Flush();

        // First flush should trigger
        _receivedChanges.Should().ContainSingle();

        // Clear for next test
        _receivedChanges.Clear();

        // Waiting for the original timer should not trigger again
        Thread.Sleep(100);
        _receivedChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task FileChanged_MultipleDebounceWindows()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromMilliseconds(50),
            changes => _receivedChanges.Add(changes));

        // First batch
        _debouncer.FileChanged("/path/file1.cs");
        await Task.Delay(100);

        // Second batch
        _debouncer.FileChanged("/path/file2.cs");
        await Task.Delay(100);

        _receivedChanges.Should().HaveCount(2);
        _receivedChanges[0].Should().ContainSingle().Which.FilePath.Should().Be("/path/file1.cs");
        _receivedChanges[1].Should().ContainSingle().Which.FilePath.Should().Be("/path/file2.cs");
    }

    [Fact]
    public void Dispose_StopsProcessing()
    {
        _debouncer = new FileChangeDebouncer(
            TimeSpan.FromMilliseconds(50),
            changes => _receivedChanges.Add(changes));

        _debouncer.FileChanged("/path/file.cs");
        _debouncer.Dispose();

        // Wait for what would have been the debounce window
        Thread.Sleep(100);

        // Should not have received any changes
        _receivedChanges.Should().BeEmpty();
    }
}

public class FileChangeInfoTests
{
    [Fact]
    public void FileName_ReturnsFileNameWithoutPath()
    {
        var info = new FileChangeInfo("/path/to/file.cs", WatcherChangeTypes.Changed, DateTimeOffset.UtcNow);

        info.FileName.Should().Be("file.cs");
    }

    [Fact]
    public void Directory_ReturnsDirectoryPath()
    {
        var info = new FileChangeInfo("/path/to/file.cs", WatcherChangeTypes.Changed, DateTimeOffset.UtcNow);

        info.Directory.Should().Be("/path/to");
    }

    [Fact]
    public void Description_ReturnsHumanReadableString()
    {
        var path = "/path/to/file.cs";

        new FileChangeInfo(path, WatcherChangeTypes.Created, DateTimeOffset.UtcNow)
            .Description.Should().Be($"Created: {path}");

        new FileChangeInfo(path, WatcherChangeTypes.Changed, DateTimeOffset.UtcNow)
            .Description.Should().Be($"Changed: {path}");

        new FileChangeInfo(path, WatcherChangeTypes.Deleted, DateTimeOffset.UtcNow)
            .Description.Should().Be($"Deleted: {path}");

        new FileChangeInfo(path, WatcherChangeTypes.Renamed, DateTimeOffset.UtcNow)
            .Description.Should().Be($"Renamed: {path}");
    }
}
