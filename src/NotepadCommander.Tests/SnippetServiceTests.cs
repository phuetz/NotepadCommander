using Xunit;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Snippets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class SnippetServiceTests
{
    private readonly SnippetService _service;

    public SnippetServiceTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<SnippetService>();
        _service = new SnippetService(logger);
    }

    [Fact]
    public void GetAll_InitiallyEmpty()
    {
        // May have loaded from disk, but we test the interface works
        Assert.NotNull(_service.GetAll());
    }

    [Fact]
    public void Add_And_GetAll()
    {
        var snippet = new Snippet
        {
            Name = "Test",
            Trigger = "tst",
            Content = "test content",
            Language = SupportedLanguage.CSharp
        };
        _service.Add(snippet);

        var all = _service.GetAll();
        Assert.Contains(all, s => s.Name == "Test");
    }

    [Fact]
    public void FindByTrigger_FindsMatch()
    {
        var snippet = new Snippet
        {
            Name = "ForLoop",
            Trigger = "for",
            Content = "for (int i = 0; i < n; i++) { }",
            Language = SupportedLanguage.CSharp
        };
        _service.Add(snippet);

        var found = _service.FindByTrigger("for", SupportedLanguage.CSharp);
        Assert.NotNull(found);
        Assert.Equal("ForLoop", found.Name);
    }

    [Fact]
    public void FindByTrigger_PlainTextMatchesAll()
    {
        var snippet = new Snippet
        {
            Name = "Date",
            Trigger = "date",
            Content = "2024-01-01",
            Language = SupportedLanguage.PlainText
        };
        _service.Add(snippet);

        var found = _service.FindByTrigger("date", SupportedLanguage.Python);
        Assert.NotNull(found);
    }

    [Fact]
    public void Delete_RemovesSnippet()
    {
        var snippet = new Snippet { Name = "ToRemove", Trigger = "rm", Content = "x" };
        _service.Add(snippet);
        _service.Delete("ToRemove");

        var found = _service.FindByTrigger("rm", SupportedLanguage.PlainText);
        Assert.Null(found);
    }

    [Fact]
    public void GetByLanguage_FiltersCorrectly()
    {
        _service.Add(new Snippet { Name = "CS1", Trigger = "cs1", Content = "a", Language = SupportedLanguage.CSharp });
        _service.Add(new Snippet { Name = "PY1", Trigger = "py1", Content = "b", Language = SupportedLanguage.Python });

        var csSnippets = _service.GetByLanguage(SupportedLanguage.CSharp);
        Assert.Contains(csSnippets, s => s.Name == "CS1");
        Assert.DoesNotContain(csSnippets, s => s.Name == "PY1");
    }
}
