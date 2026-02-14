using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
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
    private MainWindowViewModel? _mainViewModel;
    private bool _editorInitialized;

    public EditorControl()
    {
        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("Editor");

        if (_textEditor != null)
            InitializeEditor();

        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeEditor()
    {
        if (_textEditor == null || _editorInitialized) return;
        _editorInitialized = true;

        // TextMate installation is deferred to ApplyViewSettings (when theme info is available)
        // or to OnLoaded as a fallback
        _textEditor.TextChanged += OnTextChanged;
        _textEditor.TextArea.SelectionChanged += OnSelectionChanged;
        _textEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

        // Ensure a valid font size even if the binding fails
        if (_textEditor.FontSize <= 0)
            _textEditor.FontSize = 14;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_textEditor == null)
        {
            _textEditor = this.FindControl<TextEditor>("Editor")
                          ?? this.GetVisualDescendants().OfType<TextEditor>().FirstOrDefault();

            if (_textEditor != null)
                InitializeEditor();
        }

        // Ensure TextMate is set up if ConnectToMainViewModel hasn't done it yet
        if (_textEditor != null && _textMateInstallation == null)
        {
            _registryOptions = new RegistryOptions(ThemeName.LightPlus);
            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
        }

        // Load pending content
        if (_textEditor != null && _currentViewModel != null)
            LoadContent(_currentViewModel);

        // Focus the editor so the cursor is visible and typing works immediately
        _textEditor?.TextArea.Focus();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_textEditor == null)
        {
            _textEditor = this.FindControl<TextEditor>("Editor");
            if (_textEditor != null)
                InitializeEditor();
        }

        ConnectToMainViewModel();
    }

    private void ConnectToMainViewModel()
    {
        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is MainWindowViewModel mainVm && _mainViewModel != mainVm)
        {
            if (_mainViewModel != null)
                DisconnectFromMainViewModel();

            _mainViewModel = mainVm;

            _mainViewModel.UndoRequested += OnUndo;
            _mainViewModel.RedoRequested += OnRedo;
            _mainViewModel.CutRequested += OnCut;
            _mainViewModel.CopyRequested += OnCopy;
            _mainViewModel.PasteRequested += OnPaste;
            _mainViewModel.SelectionRequested += OnSelectionRequested;
            _mainViewModel.ToggleCommentRequested += OnToggleComment;
            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

            _mainViewModel.FindReplaceViewModel.GetCurrentText = () => _textEditor?.Text ?? string.Empty;
            _mainViewModel.FindReplaceViewModel.GetCurrentOffset = () => _textEditor?.CaretOffset ?? 0;
            _mainViewModel.FindReplaceViewModel.NavigateToResult += OnNavigateToResult;
            _mainViewModel.FindReplaceViewModel.ReplaceAllText += OnReplaceAllText;

            ApplyViewSettings();

            // Re-apply content after theme setup (InstallTextMate may reset the document)
            if (_currentViewModel != null && _textEditor != null)
            {
                _isUpdatingFromViewModel = true;
                _textEditor.Text = _currentViewModel.Content;
                _isUpdatingFromViewModel = false;
            }
        }
    }

    private void DisconnectFromMainViewModel()
    {
        if (_mainViewModel == null) return;
        _mainViewModel.UndoRequested -= OnUndo;
        _mainViewModel.RedoRequested -= OnRedo;
        _mainViewModel.CutRequested -= OnCut;
        _mainViewModel.CopyRequested -= OnCopy;
        _mainViewModel.PasteRequested -= OnPaste;
        _mainViewModel.SelectionRequested -= OnSelectionRequested;
        _mainViewModel.ToggleCommentRequested -= OnToggleComment;
        _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
        _mainViewModel.FindReplaceViewModel.NavigateToResult -= OnNavigateToResult;
        _mainViewModel.FindReplaceViewModel.ReplaceAllText -= OnReplaceAllText;
        _mainViewModel.FindReplaceViewModel.GetCurrentText = null;
        _mainViewModel.FindReplaceViewModel.GetCurrentOffset = null;
    }

    private void OnMainViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.ShowLineNumbers)
            or nameof(MainWindowViewModel.WordWrap)
            or nameof(MainWindowViewModel.CurrentTheme)
            or nameof(MainWindowViewModel.HighlightCurrentLine)
            or nameof(MainWindowViewModel.ShowWhitespace))
        {
            ApplyViewSettings();
        }
    }

    private void ApplyViewSettings()
    {
        if (_textEditor == null || _mainViewModel == null) return;
        _textEditor.ShowLineNumbers = _mainViewModel.ShowLineNumbers;
        _textEditor.WordWrap = _mainViewModel.WordWrap;

        var isDark = _mainViewModel.CurrentTheme == "Dark";
        _textEditor.Background = isDark
            ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"))
            : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
        _textEditor.Foreground = isDark
            ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D4D4D4"))
            : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#333333"));

        // Current line highlight
        _textEditor.Options.HighlightCurrentLine = _mainViewModel.HighlightCurrentLine;

        // Show whitespace
        _textEditor.Options.ShowEndOfLine = _mainViewModel.ShowWhitespace;
        _textEditor.Options.ShowSpaces = _mainViewModel.ShowWhitespace;
        _textEditor.Options.ShowTabs = _mainViewModel.ShowWhitespace;

        var themeName = isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;
        _registryOptions = new RegistryOptions(themeName);

        // Dispose old installation before creating new one
        _textMateInstallation?.Dispose();
        _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);

        if (_currentViewModel != null)
            ApplySyntaxHighlighting(_currentViewModel.Language);
    }

    private void OnUndo()
    {
        if (_textEditor?.Document.UndoStack.CanUndo == true)
            _textEditor.Document.UndoStack.Undo();
    }

    private void OnRedo()
    {
        if (_textEditor?.Document.UndoStack.CanRedo == true)
            _textEditor.Document.UndoStack.Redo();
    }

    private void OnCut() => _textEditor?.Cut();
    private void OnCopy() => _textEditor?.Copy();

    private async void OnPaste()
    {
        if (_textEditor == null) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        var text = await clipboard.GetTextAsync();
        if (text != null)
        {
            _textEditor.TextArea.Selection.ReplaceSelectionWithText(text);
        }
    }

    private void OnSelectionRequested(int offset, int length)
    {
        if (_textEditor == null) return;
        _textEditor.Select(offset, length);
        var loc = _textEditor.Document.GetLocation(offset);
        _textEditor.ScrollTo(loc.Line, loc.Column);
    }

    private void OnNavigateToResult(SearchResult result)
    {
        if (_textEditor == null) return;
        var offset = _textEditor.Document.GetOffset(result.Line, result.Column);
        _textEditor.Select(offset, result.Length);
        _textEditor.ScrollTo(result.Line, result.Column);
        _textEditor.TextArea.Focus();
    }

    private void OnReplaceAllText(string newText)
    {
        if (_textEditor == null) return;
        _isUpdatingFromViewModel = true;
        _textEditor.Text = newText;
        _isUpdatingFromViewModel = false;
        if (_currentViewModel != null)
            _currentViewModel.Content = newText;
    }

    private void OnToggleComment(string _)
    {
        if (_textEditor == null || _currentViewModel == null || _mainViewModel == null) return;

        var textArea = _textEditor.TextArea;
        var doc = _textEditor.Document;

        // Determine affected lines
        int startLine, endLine;
        if (textArea.Selection.IsEmpty)
        {
            startLine = endLine = textArea.Caret.Line;
        }
        else
        {
            var selStart = textArea.Selection.SurroundingSegment;
            if (selStart == null) return;
            startLine = doc.GetLineByOffset(selStart.Offset).LineNumber;
            endLine = doc.GetLineByOffset(selStart.EndOffset).LineNumber;
        }

        // Get text of affected lines
        var firstDocLine = doc.GetLineByNumber(startLine);
        var lastDocLine = doc.GetLineByNumber(endLine);
        var offset = firstDocLine.Offset;
        var length = lastDocLine.EndOffset - firstDocLine.Offset;
        var linesText = doc.GetText(offset, length);

        // Toggle comment
        var commented = _mainViewModel.CommentService.ToggleComment(linesText, _currentViewModel.Language);

        // Replace in document
        _isUpdatingFromViewModel = true;
        doc.Replace(offset, length, commented);
        _isUpdatingFromViewModel = false;

        _currentViewModel.Content = _textEditor.Text ?? string.Empty;
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_textEditor == null || _mainViewModel == null) return;
        var selection = _textEditor.TextArea.Selection;
        _mainViewModel.SelectedText = selection.IsEmpty ? null : _textEditor.SelectedText;
    }

    private void LoadContent(DocumentTabViewModel viewModel)
    {
        if (_textEditor == null) return;

        _isUpdatingFromViewModel = true;
        _textEditor.Text = viewModel.Content;
        _isUpdatingFromViewModel = false;

        ApplySyntaxHighlighting(viewModel.Language);
        viewModel.PropertyChanged -= OnViewModelPropertyChanged; // prevent double subscribe
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel != null)
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _currentViewModel = DataContext as DocumentTabViewModel;

        if (_currentViewModel != null && _textEditor != null)
        {
            LoadContent(_currentViewModel);
            _textEditor.TextArea.Focus();
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
        else if (e.PropertyName == nameof(DocumentTabViewModel.CursorLine) && _currentViewModel != null && _textEditor != null)
        {
            var caret = _textEditor.TextArea.Caret;
            if (caret.Line != _currentViewModel.CursorLine)
            {
                caret.Line = _currentViewModel.CursorLine;
                caret.Column = 1;
                _textEditor.ScrollTo(caret.Line, caret.Column);
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
            _textMateInstallation.SetGrammar(scopeName);
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
