namespace NotepadCommander.Core.Models;

/// <summary>
/// Modele representant un document texte ouvert dans l'editeur.
/// </summary>
public class TextDocument
{
    public string Content { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DocumentEncoding Encoding { get; set; } = DocumentEncoding.Utf8;
    public LineEndingType LineEnding { get; set; } = LineEndingType.CrLf;
    public SupportedLanguage Language { get; set; } = SupportedLanguage.PlainText;
    public bool IsModified { get; set; }
    public bool IsNew => FilePath == null;

    public string DisplayName => IsNew
        ? "Sans titre"
        : Path.GetFileName(FilePath!);

    public string Title => IsModified
        ? $"{DisplayName} *"
        : DisplayName;
}
