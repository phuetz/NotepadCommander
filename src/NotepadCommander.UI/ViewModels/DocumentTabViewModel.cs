using CommunityToolkit.Mvvm.ComponentModel;
using NotepadCommander.Core.Models;

namespace NotepadCommander.UI.ViewModels;

public partial class DocumentTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private TextDocument document;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private bool isModified;

    [ObservableProperty]
    private int cursorLine = 1;

    [ObservableProperty]
    private int cursorColumn = 1;

    [ObservableProperty]
    private int selectionLength;

    [ObservableProperty]
    private double fontSize = 14;

    public DocumentTabViewModel(TextDocument document)
    {
        Document = document;
        Content = document.Content;
    }

    public string Title => IsModified
        ? $"{Document.DisplayName} *"
        : Document.DisplayName;

    public string? FilePath => Document.FilePath;

    public DocumentEncoding Encoding
    {
        get => Document.Encoding;
        set
        {
            Document.Encoding = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EncodingDisplay));
        }
    }

    public LineEndingType LineEnding
    {
        get => Document.LineEnding;
        set
        {
            Document.LineEnding = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineEndingDisplay));
        }
    }

    public SupportedLanguage Language
    {
        get => Document.Language;
        set
        {
            Document.Language = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageDisplay));
        }
    }

    public string EncodingDisplay => Encoding switch
    {
        DocumentEncoding.Utf8 => "UTF-8",
        DocumentEncoding.Utf8Bom => "UTF-8 BOM",
        DocumentEncoding.Ascii => "ASCII",
        DocumentEncoding.Latin1 => "ISO 8859-1",
        DocumentEncoding.Utf16Le => "UTF-16 LE",
        DocumentEncoding.Utf16Be => "UTF-16 BE",
        _ => "UTF-8"
    };

    public string LineEndingDisplay => LineEnding switch
    {
        LineEndingType.CrLf => "CRLF",
        LineEndingType.Lf => "LF",
        LineEndingType.Cr => "CR",
        _ => "CRLF"
    };

    public string LanguageDisplay => Language switch
    {
        SupportedLanguage.PlainText => "Texte brut",
        SupportedLanguage.CSharp => "C#",
        SupportedLanguage.JavaScript => "JavaScript",
        SupportedLanguage.TypeScript => "TypeScript",
        SupportedLanguage.Python => "Python",
        SupportedLanguage.Java => "Java",
        SupportedLanguage.Cpp => "C++",
        SupportedLanguage.C => "C",
        SupportedLanguage.Html => "HTML",
        SupportedLanguage.Css => "CSS",
        SupportedLanguage.Xml => "XML",
        SupportedLanguage.Json => "JSON",
        SupportedLanguage.Yaml => "YAML",
        SupportedLanguage.Markdown => "Markdown",
        SupportedLanguage.Sql => "SQL",
        SupportedLanguage.PowerShell => "PowerShell",
        SupportedLanguage.Bash => "Bash",
        SupportedLanguage.Ruby => "Ruby",
        SupportedLanguage.Go => "Go",
        SupportedLanguage.Rust => "Rust",
        SupportedLanguage.Php => "PHP",
        _ => "Texte brut"
    };

    public string CursorPositionDisplay => SelectionLength > 0
        ? $"Ln {CursorLine}, Col {CursorColumn} (Sel: {SelectionLength})"
        : $"Ln {CursorLine}, Col {CursorColumn}";

    partial void OnContentChanged(string value)
    {
        if (value != Document.Content)
        {
            Document.Content = value;
            IsModified = true;
        }
    }

    partial void OnIsModifiedChanged(bool value)
    {
        Document.IsModified = value;
        OnPropertyChanged(nameof(Title));
    }

    partial void OnCursorLineChanged(int value) => OnPropertyChanged(nameof(CursorPositionDisplay));
    partial void OnCursorColumnChanged(int value) => OnPropertyChanged(nameof(CursorPositionDisplay));
    partial void OnSelectionLengthChanged(int value) => OnPropertyChanged(nameof(CursorPositionDisplay));

    public void MarkAsSaved()
    {
        IsModified = false;
        OnPropertyChanged(nameof(Title));
    }
}
