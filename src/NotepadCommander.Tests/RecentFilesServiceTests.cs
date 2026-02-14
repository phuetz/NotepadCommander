using NotepadCommander.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NotepadCommander.Tests;

public class RecentFilesServiceTests
{
    private readonly RecentFilesService _service;

    public RecentFilesServiceTests()
    {
        var logger = new LoggerFactory().CreateLogger<RecentFilesService>();
        _service = new RecentFilesService(logger);
        _service.Clear();
    }

    [Fact]
    public void AddFile_AddsToRecentFiles()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.AddFile(tempFile);
            Assert.Contains(_service.RecentFiles, f => f == Path.GetFullPath(tempFile));
        }
        finally
        {
            _service.Clear();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void AddFile_MostRecentFirst()
    {
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        try
        {
            _service.AddFile(file1);
            _service.AddFile(file2);

            Assert.Equal(Path.GetFullPath(file2), _service.RecentFiles[0]);
        }
        finally
        {
            _service.Clear();
            File.Delete(file1);
            File.Delete(file2);
        }
    }

    [Fact]
    public void AddFile_NoDuplicates()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.AddFile(tempFile);
            _service.AddFile(tempFile);

            var normalized = Path.GetFullPath(tempFile);
            Assert.Single(_service.RecentFiles.Where(f => f == normalized));
        }
        finally
        {
            _service.Clear();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void RemoveFile_RemovesFromList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.AddFile(tempFile);
            _service.RemoveFile(tempFile);

            Assert.DoesNotContain(_service.RecentFiles, f => f == Path.GetFullPath(tempFile));
        }
        finally
        {
            _service.Clear();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void PinFile_AddsToPin()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.AddFile(tempFile);
            _service.PinFile(tempFile);

            Assert.Contains(_service.PinnedFiles, f => f == Path.GetFullPath(tempFile));
        }
        finally
        {
            _service.Clear();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void UnpinFile_RemovesFromPinned()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.AddFile(tempFile);
            _service.PinFile(tempFile);
            _service.UnpinFile(tempFile);

            Assert.DoesNotContain(_service.PinnedFiles, f => f == Path.GetFullPath(tempFile));
        }
        finally
        {
            _service.Clear();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.AddFile(tempFile);
            _service.PinFile(tempFile);
            _service.Clear();

            Assert.Empty(_service.RecentFiles);
            Assert.Empty(_service.PinnedFiles);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
