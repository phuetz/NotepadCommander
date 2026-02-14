using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.TextTransform;
using Xunit;

namespace NotepadCommander.Tests;

public class CommentServiceTests
{
    private readonly CommentService _service = new();

    [Fact]
    public void GetCommentPrefix_CSharp() => Assert.Equal("//", _service.GetCommentPrefix(SupportedLanguage.CSharp));

    [Fact]
    public void GetCommentPrefix_Python() => Assert.Equal("#", _service.GetCommentPrefix(SupportedLanguage.Python));

    [Fact]
    public void GetCommentPrefix_Sql() => Assert.Equal("--", _service.GetCommentPrefix(SupportedLanguage.Sql));

    [Fact]
    public void GetCommentPrefix_PlainText() => Assert.Null(_service.GetCommentPrefix(SupportedLanguage.PlainText));

    [Fact]
    public void CommentLines_CSharp()
    {
        var input = "var x = 1;\nvar y = 2;";
        var result = _service.CommentLines(input, SupportedLanguage.CSharp);
        var lines = result.Split(Environment.NewLine);

        Assert.StartsWith("// ", lines[0]);
        Assert.StartsWith("// ", lines[1]);
    }

    [Fact]
    public void UncommentLines_CSharp()
    {
        var input = "// var x = 1;\n// var y = 2;";
        var result = _service.UncommentLines(input, SupportedLanguage.CSharp);
        var lines = result.Split(Environment.NewLine);

        Assert.Equal("var x = 1;", lines[0]);
        Assert.Equal("var y = 2;", lines[1]);
    }

    [Fact]
    public void ToggleComment_Comments_WhenNotCommented()
    {
        var input = "line1\nline2";
        var result = _service.ToggleComment(input, SupportedLanguage.CSharp);

        Assert.Contains("//", result);
    }

    [Fact]
    public void ToggleComment_Uncomments_WhenAllCommented()
    {
        var input = "// line1\n// line2";
        var result = _service.ToggleComment(input, SupportedLanguage.CSharp);
        var lines = result.Split(Environment.NewLine);

        Assert.Equal("line1", lines[0]);
        Assert.Equal("line2", lines[1]);
    }

    [Fact]
    public void ToggleComment_PreservesEmptyLines()
    {
        var input = "line1\n\nline2";
        var result = _service.ToggleComment(input, SupportedLanguage.Python);

        Assert.Contains("# line1", result);
        Assert.Contains("# line2", result);
    }

    [Fact]
    public void CommentLines_PlainText_NoChange()
    {
        var input = "plain text";
        var result = _service.CommentLines(input, SupportedLanguage.PlainText);
        var lines = result.Split(Environment.NewLine);
        Assert.Equal("plain text", lines[0]);
    }
}
