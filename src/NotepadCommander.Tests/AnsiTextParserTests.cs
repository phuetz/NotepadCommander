using Xunit;
using NotepadCommander.UI.Controls;

namespace NotepadCommander.Tests;

public class AnsiTextParserTests
{
    [Fact]
    public void Parse_PlainText_ReturnsSingleSpan()
    {
        var spans = AnsiTextParser.Parse("hello world");

        Assert.Single(spans);
        Assert.Equal("hello world", spans[0].Text);
        Assert.Null(spans[0].Foreground);
        Assert.False(spans[0].IsBold);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var spans = AnsiTextParser.Parse("");
        Assert.Empty(spans);
    }

    [Fact]
    public void Parse_NullString_ReturnsEmpty()
    {
        var spans = AnsiTextParser.Parse(null!);
        Assert.Empty(spans);
    }

    [Fact]
    public void Parse_RedText_SetsCorrectForeground()
    {
        var spans = AnsiTextParser.Parse("\x1b[31mhello\x1b[0m");

        Assert.Single(spans);
        Assert.Equal("hello", spans[0].Text);
        Assert.NotNull(spans[0].Foreground);
    }

    [Fact]
    public void Parse_BoldText_SetsBold()
    {
        var spans = AnsiTextParser.Parse("\x1b[1mhello\x1b[0m");

        Assert.Single(spans);
        Assert.Equal("hello", spans[0].Text);
        Assert.True(spans[0].IsBold);
    }

    [Fact]
    public void Parse_Reset_ClearsFormatting()
    {
        var spans = AnsiTextParser.Parse("\x1b[31mred\x1b[0mnormal");

        Assert.Equal(2, spans.Count);
        Assert.NotNull(spans[0].Foreground);
        Assert.Null(spans[1].Foreground);
        Assert.Equal("normal", spans[1].Text);
    }

    [Fact]
    public void Parse_BrightColors_AreParsed()
    {
        var spans = AnsiTextParser.Parse("\x1b[92mgreen\x1b[0m");

        Assert.Single(spans);
        Assert.Equal("green", spans[0].Text);
        Assert.NotNull(spans[0].Foreground);
    }

    [Fact]
    public void Parse_MixedTextAndAnsi()
    {
        var spans = AnsiTextParser.Parse("start \x1b[34mblue\x1b[0m end");

        Assert.Equal(3, spans.Count);
        Assert.Equal("start ", spans[0].Text);
        Assert.Equal("blue", spans[1].Text);
        Assert.Equal(" end", spans[2].Text);
    }

    [Fact]
    public void Parse_MultipleCodesInOneSequence()
    {
        var spans = AnsiTextParser.Parse("\x1b[1;33mhello\x1b[0m");

        Assert.Single(spans);
        Assert.True(spans[0].IsBold);
        Assert.NotNull(spans[0].Foreground);
    }
}
