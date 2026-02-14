using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.TextTransform;

public interface ICommentService
{
    string ToggleComment(string text, SupportedLanguage language);
    string CommentLines(string text, SupportedLanguage language);
    string UncommentLines(string text, SupportedLanguage language);
    string? GetCommentPrefix(SupportedLanguage language);
}
