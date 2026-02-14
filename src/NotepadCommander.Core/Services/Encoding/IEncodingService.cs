using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Encoding;

public interface IEncodingService
{
    byte[] ConvertEncoding(byte[] data, DocumentEncoding from, DocumentEncoding to);
    System.Text.Encoding GetEncoding(DocumentEncoding encoding);
}
