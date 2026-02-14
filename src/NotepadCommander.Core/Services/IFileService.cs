using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services;

public interface IFileService
{
    TextDocument CreateNew();
    Task<TextDocument> OpenAsync(string filePath);
    Task SaveAsync(TextDocument document);
    Task SaveAsAsync(TextDocument document, string filePath);
    DocumentEncoding DetectEncoding(byte[] data);
    LineEndingType DetectLineEnding(string content);
    SupportedLanguage DetectLanguage(string filePath);
    System.Text.Encoding GetDotNetEncoding(DocumentEncoding encoding);
}
