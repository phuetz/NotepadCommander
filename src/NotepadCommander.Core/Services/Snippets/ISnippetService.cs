using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Snippets;

public interface ISnippetService
{
    List<Snippet> GetAll();
    List<Snippet> GetByLanguage(SupportedLanguage language);
    Snippet? FindByTrigger(string trigger, SupportedLanguage language);
    void Add(Snippet snippet);
    void Update(Snippet snippet);
    void Delete(string name);
    void Load();
    void Save();
}
