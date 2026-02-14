using NotepadCommander.Core.Services;
using Xunit;

namespace NotepadCommander.Tests;

public class SearchReplaceServiceTests
{
    private readonly SearchReplaceService _service = new();

    [Fact]
    public void FindAll_SimpleSearch()
    {
        var text = "Hello World Hello World";
        var results = _service.FindAll(text, "Hello", false, true, false);

        Assert.Equal(2, results.Count);
        Assert.Equal(0, results[0].Index);
        Assert.Equal(12, results[1].Index);
    }

    [Fact]
    public void FindAll_CaseInsensitive()
    {
        var text = "Hello HELLO hello";
        var results = _service.FindAll(text, "hello", false, false, false);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void FindAll_CaseSensitive()
    {
        var text = "Hello HELLO hello";
        var results = _service.FindAll(text, "Hello", false, true, false);

        Assert.Single(results);
        Assert.Equal(0, results[0].Index);
    }

    [Fact]
    public void FindAll_WholeWord()
    {
        var text = "cat catch category cat";
        var results = _service.FindAll(text, "cat", false, true, true);

        Assert.Equal(2, results.Count);
        Assert.Equal(0, results[0].Index);
        Assert.Equal(19, results[1].Index);
    }

    [Fact]
    public void FindAll_Regex()
    {
        var text = "foo123 bar456 baz789";
        var results = _service.FindAll(text, @"\d+", true, true, false);

        Assert.Equal(3, results.Count);
        Assert.Equal("123", results[0].Value);
        Assert.Equal("456", results[1].Value);
        Assert.Equal("789", results[2].Value);
    }

    [Fact]
    public void FindNext_WrapsAround()
    {
        var text = "Hello World";
        var result = _service.FindNext(text, "Hello", 6, false, true, false);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Index);
    }

    [Fact]
    public void FindPrevious_WrapsAround()
    {
        var text = "Hello World Hello";
        var result = _service.FindPrevious(text, "Hello", 0, false, true, false);

        Assert.NotNull(result);
        Assert.Equal(12, result!.Index);
    }

    [Fact]
    public void ReplaceAll_Simple()
    {
        var text = "Hello World Hello World";
        var result = _service.ReplaceAll(text, "Hello", "Hi", false, true, false);

        Assert.Equal("Hi World Hi World", result);
    }

    [Fact]
    public void ReplaceAll_Regex()
    {
        var text = "foo123 bar456";
        var result = _service.ReplaceAll(text, @"\d+", "XXX", true, true, false);

        Assert.Equal("fooXXX barXXX", result);
    }

    [Fact]
    public void ReplaceAllWithCount_ReturnsCorrectCount()
    {
        var text = "aaa bbb aaa bbb aaa";
        var (result, count) = _service.ReplaceAllWithCount(text, "aaa", "ccc", false, true, false);

        Assert.Equal(3, count);
        Assert.Equal("ccc bbb ccc bbb ccc", result);
    }

    [Fact]
    public void FindAll_EmptyPattern_ReturnsEmpty()
    {
        var results = _service.FindAll("some text", "", false, true, false);
        Assert.Empty(results);
    }

    [Fact]
    public void FindAll_LineAndColumn_Correct()
    {
        var text = "line1\nline2\nline3 Hello";
        var results = _service.FindAll(text, "Hello", false, true, false);

        Assert.Single(results);
        Assert.Equal(3, results[0].Line);
        Assert.Equal(7, results[0].Column);
    }
}
