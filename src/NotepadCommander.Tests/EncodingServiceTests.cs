using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Encoding;
using Xunit;

namespace NotepadCommander.Tests;

public class EncodingServiceTests
{
    private readonly EncodingService _service = new();

    [Fact]
    public void ConvertEncoding_Utf8_To_Utf16()
    {
        var text = "Hello, World!";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var utf16Bytes = _service.ConvertEncoding(utf8Bytes, DocumentEncoding.Utf8, DocumentEncoding.Utf16Le);

        var result = System.Text.Encoding.Unicode.GetString(utf16Bytes);
        Assert.Equal(text, result);
    }

    [Fact]
    public void GetEncoding_ReturnsCorrect()
    {
        Assert.IsType<System.Text.UTF8Encoding>(_service.GetEncoding(DocumentEncoding.Utf8));
        Assert.IsType<System.Text.UTF8Encoding>(_service.GetEncoding(DocumentEncoding.Utf8Bom));
        Assert.Same(System.Text.Encoding.ASCII, _service.GetEncoding(DocumentEncoding.Ascii));
        Assert.Same(System.Text.Encoding.Latin1, _service.GetEncoding(DocumentEncoding.Latin1));
    }
}
