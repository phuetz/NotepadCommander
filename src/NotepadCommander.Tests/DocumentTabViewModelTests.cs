using NotepadCommander.Core.Models;
using NotepadCommander.UI.ViewModels;
using Xunit;

namespace NotepadCommander.Tests;

public class DocumentTabViewModelTests
{
    [Fact]
    public void Title_NewDocument_ShowsSansTitre()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);

        Assert.Equal("Sans titre", vm.Title);
    }

    [Fact]
    public void Title_WithFilePath_ShowsFileName()
    {
        var doc = new TextDocument { FilePath = "/path/to/file.cs" };
        var vm = new DocumentTabViewModel(doc);

        Assert.Equal("file.cs", vm.Title);
    }

    [Fact]
    public void Title_Modified_ShowsAsterisk()
    {
        var doc = new TextDocument { FilePath = "/path/to/file.cs" };
        var vm = new DocumentTabViewModel(doc);

        vm.Content = "modified";

        Assert.True(vm.IsModified);
        Assert.Equal("file.cs *", vm.Title);
    }

    [Fact]
    public void MarkAsSaved_ClearsModifiedFlag()
    {
        var doc = new TextDocument { FilePath = "/path/to/file.cs" };
        var vm = new DocumentTabViewModel(doc);

        vm.Content = "modified";
        Assert.True(vm.IsModified);

        vm.MarkAsSaved();
        Assert.False(vm.IsModified);
        Assert.Equal("file.cs", vm.Title);
    }

    [Fact]
    public void CursorPositionDisplay_Default()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);

        Assert.Equal("Ln 1, Col 1", vm.CursorPositionDisplay);
    }

    [Fact]
    public void CursorPositionDisplay_WithSelection()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);
        vm.CursorLine = 5;
        vm.CursorColumn = 10;
        vm.SelectionLength = 15;

        Assert.Equal("Ln 5, Col 10 (Sel: 15)", vm.CursorPositionDisplay);
    }

    [Fact]
    public void EncodingDisplay_ReturnsCorrectLabels()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);

        vm.Encoding = DocumentEncoding.Utf8;
        Assert.Equal("UTF-8", vm.EncodingDisplay);

        vm.Encoding = DocumentEncoding.Utf8Bom;
        Assert.Equal("UTF-8 BOM", vm.EncodingDisplay);

        vm.Encoding = DocumentEncoding.Latin1;
        Assert.Equal("ISO 8859-1", vm.EncodingDisplay);
    }

    [Fact]
    public void LineEndingDisplay_ReturnsCorrectLabels()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);

        vm.LineEnding = LineEndingType.CrLf;
        Assert.Equal("CRLF", vm.LineEndingDisplay);

        vm.LineEnding = LineEndingType.Lf;
        Assert.Equal("LF", vm.LineEndingDisplay);

        vm.LineEnding = LineEndingType.Cr;
        Assert.Equal("CR", vm.LineEndingDisplay);
    }

    [Fact]
    public void LanguageDisplay_ReturnsCorrectLabels()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);

        vm.Language = SupportedLanguage.CSharp;
        Assert.Equal("C#", vm.LanguageDisplay);

        vm.Language = SupportedLanguage.PlainText;
        Assert.Equal("Texte brut", vm.LanguageDisplay);

        vm.Language = SupportedLanguage.Json;
        Assert.Equal("JSON", vm.LanguageDisplay);
    }

    [Fact]
    public void FontSize_DefaultIs14()
    {
        var doc = new TextDocument();
        var vm = new DocumentTabViewModel(doc);

        Assert.Equal(14, vm.FontSize);
    }
}
