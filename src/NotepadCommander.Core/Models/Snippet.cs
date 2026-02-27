namespace NotepadCommander.Core.Models;

public class Snippet
{
    public string Name { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public SupportedLanguage Language { get; set; } = SupportedLanguage.PlainText;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
