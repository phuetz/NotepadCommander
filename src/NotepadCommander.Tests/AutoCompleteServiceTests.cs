using Xunit;
using NotepadCommander.Core.Services.AutoComplete;
using NotepadCommander.Core.Models;

namespace NotepadCommander.Tests;

public class AutoCompleteServiceTests
{
    private readonly AutoCompleteService _service = new();

    [Fact]
    public void GetSuggestions_EmptyText_ReturnsEmpty()
    {
        var result = _service.GetSuggestions("", 0, SupportedLanguage.PlainText);
        Assert.Empty(result);
    }

    [Fact]
    public void GetSuggestions_ShortPrefix_ReturnsEmpty()
    {
        var result = _service.GetSuggestions("a", 1, SupportedLanguage.CSharp);
        Assert.Empty(result);
    }

    [Fact]
    public void GetSuggestions_CSharpKeyword_FindsMatch()
    {
        var result = _service.GetSuggestions("pub", 3, SupportedLanguage.CSharp);
        Assert.Contains("public", result);
    }

    [Fact]
    public void GetSuggestions_PythonKeyword_FindsMatch()
    {
        var result = _service.GetSuggestions("def", 3, SupportedLanguage.Python);
        Assert.Contains("def", result);
    }

    [Fact]
    public void GetSuggestions_DocumentWords_IncludesWords()
    {
        var text = "myVariable = 42\nmyFunction = func\nmy";
        var result = _service.GetSuggestions(text, text.Length, SupportedLanguage.PlainText);
        Assert.Contains("myVariable", result);
        Assert.Contains("myFunction", result);
    }

    [Fact]
    public void GetSuggestions_LimitsResults()
    {
        var result = _service.GetSuggestions("st", 2, SupportedLanguage.CSharp, maxResults: 3);
        Assert.True(result.Count <= 3);
    }

    [Fact]
    public void GetCurrentWord_ExtractsWord()
    {
        var word = AutoCompleteService.GetCurrentWord("hello world", 5);
        Assert.Equal("hello", word);
    }

    [Fact]
    public void GetCurrentWord_WithUnderscore()
    {
        var word = AutoCompleteService.GetCurrentWord("my_var", 6);
        Assert.Equal("my_var", word);
    }

    [Fact]
    public void ExtractWords_FindsAllWords()
    {
        var words = AutoCompleteService.ExtractWords("hello world foo bar");
        Assert.Contains("hello", words);
        Assert.Contains("world", words);
        Assert.Contains("foo", words);
        Assert.Contains("bar", words);
    }

    [Fact]
    public void ExtractWords_IgnoresShortWords()
    {
        var words = AutoCompleteService.ExtractWords("a bb ccc");
        Assert.DoesNotContain("a", words);
        Assert.DoesNotContain("bb", words);
        Assert.Contains("ccc", words);
    }
}
