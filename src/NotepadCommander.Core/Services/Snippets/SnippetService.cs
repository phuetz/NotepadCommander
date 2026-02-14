using System.Text.Json;
using NotepadCommander.Core.Models;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.Snippets;

public class SnippetService : ISnippetService
{
    private readonly ILogger<SnippetService> _logger;
    private readonly List<Snippet> _snippets = new();

    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotepadCommander");

    private static readonly string SnippetsPath = Path.Combine(DataDir, "snippets.json");

    public SnippetService(ILogger<SnippetService> logger)
    {
        _logger = logger;
        Load();
    }

    public List<Snippet> GetAll() => new(_snippets);

    public List<Snippet> GetByLanguage(SupportedLanguage language) =>
        _snippets.Where(s => s.Language == language || s.Language == SupportedLanguage.PlainText).ToList();

    public Snippet? FindByTrigger(string trigger, SupportedLanguage language) =>
        _snippets.FirstOrDefault(s => s.Trigger == trigger &&
            (s.Language == language || s.Language == SupportedLanguage.PlainText));

    public void Add(Snippet snippet)
    {
        _snippets.Add(snippet);
        Save();
    }

    public void Update(Snippet snippet)
    {
        var index = _snippets.FindIndex(s => s.Name == snippet.Name);
        if (index >= 0)
        {
            _snippets[index] = snippet;
            Save();
        }
    }

    public void Delete(string name)
    {
        _snippets.RemoveAll(s => s.Name == name);
        Save();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SnippetsPath)) return;
            var json = File.ReadAllText(SnippetsPath);
            var loaded = JsonSerializer.Deserialize<List<Snippet>>(json);
            if (loaded != null)
            {
                _snippets.Clear();
                _snippets.AddRange(loaded);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de charger les snippets");
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(_snippets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SnippetsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de sauvegarder les snippets");
        }
    }
}
