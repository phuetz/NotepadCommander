using Xunit;
using NotepadCommander.Core.Services.Git;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class GitServiceTests
{
    private readonly GitService _service;

    public GitServiceTests()
    {
        _service = new GitService(NullLogger<GitService>.Instance);
    }

    [Fact]
    public void IsInGitRepository_ReturnsFalse_ForNonGitDir()
    {
        // Use a temp directory that is not a git repo
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(tempFile, "hello");

        try
        {
            var result = _service.IsInGitRepository(tempFile);
            Assert.False(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsInGitRepository_ReturnsTrue_ForGitDir()
    {
        // The current repo should be a git repo
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "NotepadCommander.Tests.csproj");
        if (!File.Exists(filePath))
        {
            // Fallback: use any file in the repo
            filePath = typeof(GitServiceTests).Assembly.Location;
        }

        var result = _service.IsInGitRepository(filePath);
        Assert.True(result);
    }

    [Fact]
    public void GetModifiedLines_ReturnsEmpty_ForNonGitFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(tempFile, "hello");

        try
        {
            var result = _service.GetModifiedLines(tempFile);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
