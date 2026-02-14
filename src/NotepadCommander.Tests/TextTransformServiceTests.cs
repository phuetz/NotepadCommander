using NotepadCommander.Core.Services.TextTransform;
using Xunit;

namespace NotepadCommander.Tests;

public class TextTransformServiceTests
{
    private readonly TextTransformService _service = new();

    [Fact]
    public void SortLines_Ascending()
    {
        var result = _service.SortLines("banana\napple\ncherry");
        var lines = result.Split(Environment.NewLine);
        Assert.Equal("apple", lines[0]);
        Assert.Equal("banana", lines[1]);
        Assert.Equal("cherry", lines[2]);
    }

    [Fact]
    public void SortLines_Descending()
    {
        var result = _service.SortLines("banana\napple\ncherry", false);
        var lines = result.Split(Environment.NewLine);
        Assert.Equal("cherry", lines[0]);
    }

    [Fact]
    public void RemoveDuplicateLines()
    {
        var result = _service.RemoveDuplicateLines("a\nb\na\nc\nb");
        var lines = result.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.Equal(new[] { "a", "b", "c" }, lines);
    }

    [Fact]
    public void ToUpperCase() => Assert.Equal("HELLO", _service.ToUpperCase("hello"));

    [Fact]
    public void ToLowerCase() => Assert.Equal("hello", _service.ToLowerCase("HELLO"));

    [Fact]
    public void ToTitleCase() => Assert.Equal("Hello World", _service.ToTitleCase("hello world"));

    [Fact]
    public void ToggleCase() => Assert.Equal("hELLO", _service.ToggleCase("Hello"));

    [Fact]
    public void TrimLines()
    {
        var result = _service.TrimLines("  hello  \n  world  ");
        var lines = result.Split(Environment.NewLine);
        Assert.Equal("hello", lines[0]);
        Assert.Equal("world", lines[1]);
    }

    [Fact]
    public void RemoveEmptyLines()
    {
        var result = _service.RemoveEmptyLines("a\n\nb\n  \nc");
        var lines = result.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public void EncodeBase64_And_Decode()
    {
        var encoded = _service.EncodeBase64("Hello, World!");
        Assert.Equal("SGVsbG8sIFdvcmxkIQ==", encoded);

        var decoded = _service.DecodeBase64(encoded);
        Assert.Equal("Hello, World!", decoded);
    }

    [Fact]
    public void DecodeBase64_Invalid_ReturnsOriginal()
    {
        var result = _service.DecodeBase64("not-valid-base64!!!");
        Assert.Equal("not-valid-base64!!!", result);
    }

    [Fact]
    public void EncodeUrl_And_Decode()
    {
        var encoded = _service.EncodeUrl("hello world&foo=bar");
        Assert.Contains("%20", encoded);

        var decoded = _service.DecodeUrl(encoded);
        Assert.Equal("hello world&foo=bar", decoded);
    }

    [Fact]
    public void FormatJson()
    {
        var input = "{\"name\":\"test\",\"value\":42}";
        var result = _service.FormatJson(input);
        Assert.Contains("\n", result);
        Assert.Contains("  ", result);
    }

    [Fact]
    public void MinifyJson()
    {
        var input = "{\n  \"name\": \"test\",\n  \"value\": 42\n}";
        var result = _service.MinifyJson(input);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void FormatJson_Invalid_ReturnsOriginal()
    {
        var input = "not json";
        Assert.Equal(input, _service.FormatJson(input));
    }

    [Fact]
    public void FormatXml()
    {
        var input = "<root><child>text</child></root>";
        var result = _service.FormatXml(input);
        Assert.Contains("\n", result);
    }

    [Fact]
    public void FormatXml_Invalid_ReturnsOriginal()
    {
        var input = "not xml <>";
        Assert.Equal(input, _service.FormatXml(input));
    }

    [Fact]
    public void JoinLines()
    {
        var result = _service.JoinLines("a\nb\nc", ", ");
        Assert.Equal("a, b, c", result);
    }

    [Fact]
    public void ReverseLines()
    {
        var result = _service.ReverseLines("a\nb\nc");
        var lines = result.Split(Environment.NewLine);
        Assert.Equal("c", lines[0]);
        Assert.Equal("a", lines[2]);
    }
}
