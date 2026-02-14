using Xunit;
using NotepadCommander.Core.Services.Markdown;

namespace NotepadCommander.Tests;

public class MarkdownServiceTests
{
    private readonly MarkdownService _service = new();

    [Fact]
    public void ToHtml_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _service.ToHtml(""));
    }

    [Fact]
    public void ToHtml_NullString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _service.ToHtml(null!));
    }

    [Fact]
    public void ToHtml_Heading_GeneratesH1()
    {
        var html = _service.ToHtml("# Hello");
        Assert.Contains("<h1", html);
        Assert.Contains("Hello", html);
    }

    [Fact]
    public void ToHtml_Bold_GeneratesStrong()
    {
        var html = _service.ToHtml("**bold**");
        Assert.Contains("<strong>", html);
    }

    [Fact]
    public void ToHtml_Link_GeneratesAnchor()
    {
        var html = _service.ToHtml("[text](http://example.com)");
        Assert.Contains("<a", html);
        Assert.Contains("http://example.com", html);
    }

    [Fact]
    public void ToHtml_CodeBlock_GeneratesPreCode()
    {
        var html = _service.ToHtml("```\ncode\n```");
        Assert.Contains("<code>", html);
    }

    [Fact]
    public void ToHtml_List_GeneratesUl()
    {
        var html = _service.ToHtml("- item1\n- item2");
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>", html);
    }
}
