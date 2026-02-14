using Xunit;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.Core.Services.Compare;

namespace NotepadCommander.Tests;

public class CliTests : IDisposable
{
    private readonly string _tempDir;

    public CliTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NotepadCmdr_CLI_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void TextTransform_SortLines_UsedByCli()
    {
        var service = new TextTransformService();
        var result = service.SortLines("cherry\napple\nbanana", ascending: true);
        Assert.StartsWith("apple", result);
    }

    [Fact]
    public void TextTransform_Uppercase_UsedByCli()
    {
        var service = new TextTransformService();
        var result = service.ToUpperCase("hello");
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void CompareService_UsedByCli()
    {
        var service = new CompareService();
        var result = service.Compare("line1\nline2", "line1\nline3");
        Assert.NotEmpty(result.Lines);
    }

    [Fact]
    public void FileReadWrite_RoundTrip()
    {
        var file = Path.Combine(_tempDir, "test.txt");
        var content = "hello\nworld";
        File.WriteAllText(file, content);
        var read = File.ReadAllText(file);
        Assert.Equal(content, read);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
