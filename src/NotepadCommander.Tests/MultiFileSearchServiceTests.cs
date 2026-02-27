using Xunit;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class MultiFileSearchServiceTests : IDisposable
{
    private readonly MultiFileSearchService _service;
    private readonly string _tempDir;

    public MultiFileSearchServiceTests()
    {
        _service = new MultiFileSearchService(NullLogger<MultiFileSearchService>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), "NcSearchTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task SearchInDirectory_FindsMatch_InSingleFile()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "Hello World\nFoo Bar\nHello Again");

        var options = new MultiFileSearchOptions();
        var results = new List<FileSearchResult>();
        await foreach (var r in _service.SearchInDirectory(_tempDir, "Hello", options))
            results.Add(r);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("Hello", r.LineText));
    }

    [Fact]
    public async Task SearchInDirectory_RespectsCaseSensitive()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "Hello World\nhello world");

        var options = new MultiFileSearchOptions { CaseSensitive = true };
        var results = new List<FileSearchResult>();
        await foreach (var r in _service.SearchInDirectory(_tempDir, "Hello", options))
            results.Add(r);

        Assert.Single(results);
        Assert.Equal(1, results[0].Line);
    }

    [Fact]
    public async Task SearchInDirectory_RespectsMaxResults()
    {
        // Create a file with many matches
        var lines = Enumerable.Range(1, 100).Select(i => $"match line {i}");
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), string.Join("\n", lines));

        var options = new MultiFileSearchOptions { MaxResults = 5 };
        var results = new List<FileSearchResult>();
        await foreach (var r in _service.SearchInDirectory(_tempDir, "match", options))
            results.Add(r);

        Assert.True(results.Count <= 5);
    }

    [Fact]
    public async Task SearchInDirectory_ReturnsEmpty_ForNoMatch()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "Hello World");

        var options = new MultiFileSearchOptions();
        var results = new List<FileSearchResult>();
        await foreach (var r in _service.SearchInDirectory(_tempDir, "ZZZZZ_NO_MATCH", options))
            results.Add(r);

        Assert.Empty(results);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); }
        catch { /* cleanup best effort */ }
    }
}
