using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Encoding;

public interface ILineEndingService
{
    string ConvertLineEndings(string text, LineEndingType targetType);
    LineEndingType DetectLineEnding(string text);
}
