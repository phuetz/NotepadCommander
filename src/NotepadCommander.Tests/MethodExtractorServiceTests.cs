using Xunit;
using NotepadCommander.Core.Services.MethodExtractor;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class MethodExtractorServiceTests : IDisposable
{
    private readonly MethodExtractorService _service;
    private readonly string _tempDir;

    public MethodExtractorServiceTests()
    {
        _service = new MethodExtractorService(NullLogger<MethodExtractorService>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), "NcMethodTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ExtractMethods_FindsCSharpMethod()
    {
        var code = @"using System;

public class MyClass
{
    public void MyMethod()
    {
        Console.WriteLine(""Hello"");
    }

    public int AnotherMethod(int x)
    {
        return x * 2;
    }
}";
        File.WriteAllText(Path.Combine(_tempDir, "Test.cs"), code);

        var results = new List<NotepadCommander.Core.Models.MethodInfo>();
        await foreach (var r in _service.ExtractMethods(_tempDir, new[] { "MyMethod" }))
            results.Add(r);

        Assert.Single(results);
        Assert.Equal("MyMethod", results[0].MethodName);
        Assert.Equal("C#", results[0].Language);
        Assert.Contains("Console.WriteLine", results[0].FullBody);
    }

    [Fact]
    public async Task ExtractMethods_ReturnsEmpty_ForNonExistentDir()
    {
        var fakeDir = Path.Combine(_tempDir, "does_not_exist");

        var results = new List<NotepadCommander.Core.Models.MethodInfo>();
        await foreach (var r in _service.ExtractMethods(fakeDir, new[] { "Foo" }))
            results.Add(r);

        Assert.Empty(results);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); }
        catch { /* cleanup best effort */ }
    }
}
