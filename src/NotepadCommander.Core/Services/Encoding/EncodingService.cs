using System.Text;
using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Encoding;

public class EncodingService : IEncodingService
{
    public byte[] ConvertEncoding(byte[] data, DocumentEncoding from, DocumentEncoding to)
    {
        var sourceEncoding = GetEncoding(from);
        var targetEncoding = GetEncoding(to);

        var text = sourceEncoding.GetString(data);
        return targetEncoding.GetBytes(text);
    }

    public System.Text.Encoding GetEncoding(DocumentEncoding encoding) => encoding switch
    {
        DocumentEncoding.Utf8 => new UTF8Encoding(false),
        DocumentEncoding.Utf8Bom => new UTF8Encoding(true),
        DocumentEncoding.Ascii => System.Text.Encoding.ASCII,
        DocumentEncoding.Latin1 => System.Text.Encoding.Latin1,
        DocumentEncoding.Utf16Le => System.Text.Encoding.Unicode,
        DocumentEncoding.Utf16Be => System.Text.Encoding.BigEndianUnicode,
        _ => new UTF8Encoding(false)
    };
}
