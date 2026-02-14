using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Encoding;
using Xunit;

namespace NotepadCommander.Tests;

public class LineEndingServiceTests
{
    private readonly LineEndingService _service = new();

    [Fact]
    public void ConvertLineEndings_CrLf_To_Lf()
    {
        var input = "line1\r\nline2\r\nline3";
        var result = _service.ConvertLineEndings(input, LineEndingType.Lf);
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void ConvertLineEndings_Lf_To_CrLf()
    {
        var input = "line1\nline2\nline3";
        var result = _service.ConvertLineEndings(input, LineEndingType.CrLf);
        Assert.Equal("line1\r\nline2\r\nline3", result);
    }

    [Fact]
    public void ConvertLineEndings_Mixed_To_Lf()
    {
        var input = "line1\r\nline2\nline3\rline4";
        var result = _service.ConvertLineEndings(input, LineEndingType.Lf);
        Assert.Equal("line1\nline2\nline3\nline4", result);
    }

    [Fact]
    public void DetectLineEnding_CrLf()
    {
        var result = _service.DetectLineEnding("line1\r\nline2\r\n");
        Assert.Equal(LineEndingType.CrLf, result);
    }

    [Fact]
    public void DetectLineEnding_Lf()
    {
        var result = _service.DetectLineEnding("line1\nline2\n");
        Assert.Equal(LineEndingType.Lf, result);
    }
}
