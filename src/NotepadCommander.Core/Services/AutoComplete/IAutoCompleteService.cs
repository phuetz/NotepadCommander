using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.AutoComplete;

public interface IAutoCompleteService
{
    List<string> GetSuggestions(string text, int cursorPosition, SupportedLanguage language, int maxResults = 20);
}
