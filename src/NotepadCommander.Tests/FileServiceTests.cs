using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NotepadCommander.Tests;

public class FileServiceTests
{
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        var logger = new LoggerFactory().CreateLogger<FileService>();
        _fileService = new FileService(logger);
    }

    [Fact]
    public void CreateNew_ReturnsEmptyDocument()
    {
        var doc = _fileService.CreateNew();

        Assert.NotNull(doc);
        Assert.Empty(doc.Content);
        Assert.Null(doc.FilePath);
        Assert.True(doc.IsNew);
        Assert.False(doc.IsModified);
        Assert.Equal(SupportedLanguage.PlainText, doc.Language);
    }

    [Fact]
    public void CreateNew_SetsDefaultEncoding()
    {
        var doc = _fileService.CreateNew();

        Assert.Equal(DocumentEncoding.Utf8, doc.Encoding);
    }

    [Fact]
    public void DetectEncoding_Utf8Bom()
    {
        var data = new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var encoding = _fileService.DetectEncoding(data);

        Assert.Equal(DocumentEncoding.Utf8Bom, encoding);
    }

    [Fact]
    public void DetectEncoding_Utf16Le()
    {
        var data = new byte[] { 0xFF, 0xFE, 0x48, 0x00 };
        var encoding = _fileService.DetectEncoding(data);

        Assert.Equal(DocumentEncoding.Utf16Le, encoding);
    }

    [Fact]
    public void DetectEncoding_Utf16Be()
    {
        var data = new byte[] { 0xFE, 0xFF, 0x00, 0x48 };
        var encoding = _fileService.DetectEncoding(data);

        Assert.Equal(DocumentEncoding.Utf16Be, encoding);
    }

    [Fact]
    public void DetectEncoding_PureAscii_ReturnsUtf8()
    {
        var data = "Hello, World!"u8.ToArray();
        var encoding = _fileService.DetectEncoding(data);

        Assert.Equal(DocumentEncoding.Utf8, encoding);
    }

    [Fact]
    public void DetectLineEnding_CrLf()
    {
        var content = "line1\r\nline2\r\nline3";
        var result = _fileService.DetectLineEnding(content);

        Assert.Equal(LineEndingType.CrLf, result);
    }

    [Fact]
    public void DetectLineEnding_Lf()
    {
        var content = "line1\nline2\nline3";
        var result = _fileService.DetectLineEnding(content);

        Assert.Equal(LineEndingType.Lf, result);
    }

    [Fact]
    public void DetectLineEnding_Cr()
    {
        var content = "line1\rline2\rline3";
        var result = _fileService.DetectLineEnding(content);

        Assert.Equal(LineEndingType.Cr, result);
    }

    [Fact]
    public void DetectLanguage_CSharp()
    {
        var result = _fileService.DetectLanguage("test.cs");
        Assert.Equal(SupportedLanguage.CSharp, result);
    }

    [Fact]
    public void DetectLanguage_Json()
    {
        var result = _fileService.DetectLanguage("config.json");
        Assert.Equal(SupportedLanguage.Json, result);
    }

    [Fact]
    public void DetectLanguage_Unknown_ReturnsPlainText()
    {
        var result = _fileService.DetectLanguage("file.xyz");
        Assert.Equal(SupportedLanguage.PlainText, result);
    }

    [Fact]
    public void DetectLanguage_Xml_Variants()
    {
        Assert.Equal(SupportedLanguage.Xml, _fileService.DetectLanguage("file.xml"));
        Assert.Equal(SupportedLanguage.Xml, _fileService.DetectLanguage("file.xaml"));
        Assert.Equal(SupportedLanguage.Xml, _fileService.DetectLanguage("file.axaml"));
        Assert.Equal(SupportedLanguage.Xml, _fileService.DetectLanguage("file.csproj"));
    }

    [Fact]
    public async Task OpenAsync_And_SaveAsync_Roundtrip()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "Hello, Notepad Commander!\r\nLine 2\r\nLine 3";
            await File.WriteAllTextAsync(tempFile, content, new System.Text.UTF8Encoding(false));

            var doc = await _fileService.OpenAsync(tempFile);

            Assert.Equal(content, doc.Content);
            Assert.Equal(tempFile, doc.FilePath);
            Assert.False(doc.IsModified);
            Assert.Equal(DocumentEncoding.Utf8, doc.Encoding);
            Assert.Equal(LineEndingType.CrLf, doc.LineEnding);

            // Modifier et sauvegarder
            doc.Content = "Modified content\r\nLine 2";
            await _fileService.SaveAsync(doc);

            var savedContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal("Modified content\r\nLine 2", savedContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAsAsync_CreatesNewFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        try
        {
            var doc = _fileService.CreateNew();
            doc.Content = "New file content";

            await _fileService.SaveAsAsync(doc, tempFile);

            Assert.True(File.Exists(tempFile));
            Assert.Equal(tempFile, doc.FilePath);
            Assert.False(doc.IsModified);

            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("New file content", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void TextDocument_Title_ShowsModifiedIndicator()
    {
        var doc = new TextDocument { FilePath = "/test/file.txt" };
        Assert.Equal("file.txt", doc.Title);

        doc.IsModified = true;
        Assert.Equal("file.txt *", doc.Title);
    }

    [Fact]
    public void TextDocument_DisplayName_SansTitre_WhenNew()
    {
        var doc = new TextDocument();
        Assert.Equal("Sans titre", doc.DisplayName);
    }
}
