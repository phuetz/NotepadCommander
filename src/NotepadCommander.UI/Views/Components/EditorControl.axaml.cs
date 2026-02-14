using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using NotepadCommander.Core.Models;
using NotepadCommander.UI.ViewModels;
using TextMateSharp.Grammars;

namespace NotepadCommander.UI.Views.Components;

public partial class EditorControl : UserControl
{
    private TextEditor? _textEditor;
    private TextMate.Installation? _textMateInstallation;
    private RegistryOptions? _registryOptions;
    private bool _isUpdatingFromViewModel;
    private DocumentTabViewModel? _currentViewModel;

    public EditorControl()
    {
        InitializeComponent();
        _textEditor = this.FindControl<TextEditor>("TextEditor");

        if (_textEditor != null)
        {
            SetupTextMate();
            _textEditor.TextChanged += OnTextChanged;
        }

        DataContextChanged += OnDataContextChanged;
    }

    private void SetupTextMate()
    {
        if (_textEditor == null) return;

        _registryOptions = new RegistryOptions(ThemeName.LightPlus);
        _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _currentViewModel = DataContext as DocumentTabViewModel;

        if (_currentViewModel != null && _textEditor != null)
        {
            _isUpdatingFromViewModel = true;
            _textEditor.Text = _currentViewModel.Content;
            _isUpdatingFromViewModel = false;

            ApplySyntaxHighlighting(_currentViewModel.Language);
            _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;

            _textEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentTabViewModel.Language) && _currentViewModel != null)
        {
            ApplySyntaxHighlighting(_currentViewModel.Language);
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.Content) && _currentViewModel != null && _textEditor != null)
        {
            if (!_isUpdatingFromViewModel && _textEditor.Text != _currentViewModel.Content)
            {
                _isUpdatingFromViewModel = true;
                _textEditor.Text = _currentViewModel.Content;
                _isUpdatingFromViewModel = false;
            }
        }
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingFromViewModel || _currentViewModel == null || _textEditor == null) return;

        _isUpdatingFromViewModel = true;
        _currentViewModel.Content = _textEditor.Text ?? string.Empty;
        _isUpdatingFromViewModel = false;
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel == null || _textEditor == null) return;

        var caret = _textEditor.TextArea.Caret;
        _currentViewModel.CursorLine = caret.Line;
        _currentViewModel.CursorColumn = caret.Column;

        var selection = _textEditor.TextArea.Selection;
        _currentViewModel.SelectionLength = selection.IsEmpty ? 0 : selection.Length;
    }

    private void ApplySyntaxHighlighting(SupportedLanguage language)
    {
        if (_textMateInstallation == null || _registryOptions == null) return;

        var scopeName = GetScopeName(language);
        if (scopeName != null)
        {
            _textMateInstallation.SetGrammar(scopeName);
        }
    }

    private string? GetScopeName(SupportedLanguage language) => language switch
    {
        SupportedLanguage.CSharp => _registryOptions?.GetScopeByLanguageId("csharp"),
        SupportedLanguage.JavaScript => _registryOptions?.GetScopeByLanguageId("javascript"),
        SupportedLanguage.TypeScript => _registryOptions?.GetScopeByLanguageId("typescript"),
        SupportedLanguage.Python => _registryOptions?.GetScopeByLanguageId("python"),
        SupportedLanguage.Java => _registryOptions?.GetScopeByLanguageId("java"),
        SupportedLanguage.Cpp => _registryOptions?.GetScopeByLanguageId("cpp"),
        SupportedLanguage.C => _registryOptions?.GetScopeByLanguageId("c"),
        SupportedLanguage.Html => _registryOptions?.GetScopeByLanguageId("html"),
        SupportedLanguage.Css => _registryOptions?.GetScopeByLanguageId("css"),
        SupportedLanguage.Xml => _registryOptions?.GetScopeByLanguageId("xml"),
        SupportedLanguage.Json => _registryOptions?.GetScopeByLanguageId("json"),
        SupportedLanguage.Yaml => _registryOptions?.GetScopeByLanguageId("yaml"),
        SupportedLanguage.Markdown => _registryOptions?.GetScopeByLanguageId("markdown"),
        SupportedLanguage.Sql => _registryOptions?.GetScopeByLanguageId("sql"),
        SupportedLanguage.PowerShell => _registryOptions?.GetScopeByLanguageId("powershell"),
        SupportedLanguage.Bash => _registryOptions?.GetScopeByLanguageId("shellscript"),
        SupportedLanguage.Ruby => _registryOptions?.GetScopeByLanguageId("ruby"),
        SupportedLanguage.Go => _registryOptions?.GetScopeByLanguageId("go"),
        SupportedLanguage.Rust => _registryOptions?.GetScopeByLanguageId("rust"),
        SupportedLanguage.Php => _registryOptions?.GetScopeByLanguageId("php"),
        _ => null
    };
}
