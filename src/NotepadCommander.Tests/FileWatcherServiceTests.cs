using Xunit;
using NotepadCommander.Core.Services.FileWatcher;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class FileWatcherServiceTests : IDisposable
{
    private readonly FileWatcherService _service;
    private readonly string _tempDir;

    public FileWatcherServiceTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<FileWatcherService>();
        _service = new FileWatcherService(logger);
        _tempDir = Path.Combine(Path.GetTempPath(), "NotepadCmdr_FW_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void WatchFile_DoesNotThrow_ForValidFile()
    {
        var file = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(file, "hello");
        _service.WatchFile(file);
        // No exception = success
    }

    [Fact]
    public void WatchFile_IgnoresNonExistentDirectory()
    {
        _service.WatchFile(@"Z:\nonexistent\dir\file.txt");
        // Should not throw
    }

    [Fact]
    public void WatchFile_IgnoresDuplicate()
    {
        var file = Path.Combine(_tempDir, "dup.txt");
        File.WriteAllText(file, "content");
        _service.WatchFile(file);
        _service.WatchFile(file); // Should not throw
    }

    [Fact]
    public void UnwatchFile_RemovesWatcher()
    {
        var file = Path.Combine(_tempDir, "unwatch.txt");
        File.WriteAllText(file, "content");
        _service.WatchFile(file);
        _service.UnwatchFile(file);
        // No exception after unwatching
    }

    [Fact]
    public void UnwatchFile_DoesNotThrow_ForUnknownFile()
    {
        _service.UnwatchFile(@"C:\unknown\file.txt");
    }

    [Fact]
    public void UnwatchAll_ClearsAllWatchers()
    {
        var file1 = Path.Combine(_tempDir, "a.txt");
        var file2 = Path.Combine(_tempDir, "b.txt");
        File.WriteAllText(file1, "a");
        File.WriteAllText(file2, "b");
        _service.WatchFile(file1);
        _service.WatchFile(file2);
        _service.UnwatchAll();
    }

    [Fact]
    public void Dispose_CleansUp()
    {
        var file = Path.Combine(_tempDir, "dispose.txt");
        File.WriteAllText(file, "content");
        _service.WatchFile(file);
        _service.Dispose();
    }

    public void Dispose()
    {
        _service.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
