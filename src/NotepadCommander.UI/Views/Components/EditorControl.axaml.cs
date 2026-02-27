using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Git;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.UI.Controls;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class EditorControl : UserControl
{
    private NotepadEditor? _editor;
    private bool _isUpdatingFromViewModel;
    private DocumentTabViewModel? _currentViewModel;
    private ShellViewModel? _mainViewModel;
    private bool _editorInitialized;
    private MinimapControl? _minimapControl;

    // Event bridge for VM events → editor actions
    private EditorEventBridge? _eventBridge;
    private ITextTransformService? _textTransformService;

    public EditorControl()
    {
        InitializeComponent();

        _editor = this.FindControl<NotepadEditor>("Editor");
        _minimapControl = this.FindControl<MinimapControl>("Minimap");

        if (_editor != null)
            InitializeEditor();

        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeEditor()
    {
        if (_editor == null || _editorInitialized) return;
        _editorInitialized = true;

        _editor.TextChanged += OnTextChanged;
        _editor.TextArea.SelectionChanged += OnSelectionChanged;
        _editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

        if (_editor.FontSize <= 0)
            _editor.FontSize = 14;

        // Resolve services once
        IGitService? gitService = null;
        try { gitService = App.Services.GetService<IGitService>(); } catch { }
        try { _textTransformService = App.Services.GetService<ITextTransformService>(); } catch { }

        // Initialize NotepadEditor (TextMate + dev features)
        _editor.Initialize(gitService);

        // Event bridge (VM events → NotepadEditor actions)
        _eventBridge = new EditorEventBridge();
        _eventBridge.Initialize(
            _editor,
            () => _currentViewModel,
            flag => _isUpdatingFromViewModel = flag,
            () => _isUpdatingFromViewModel);

        try { InitializeContextMenu(); } catch { }
    }

    private void InitializeContextMenu()
    {
        if (_editor == null) return;

        var cutItem = new MenuItem { Header = "Couper (Ctrl+X)" };
        cutItem.Click += (_, _) => _editor?.Cut();

        var copyItem = new MenuItem { Header = "Copier (Ctrl+C)" };
        copyItem.Click += (_, _) => _editor?.Copy();

        var pasteItem = new MenuItem { Header = "Coller (Ctrl+V)" };
        pasteItem.Click += async (_, _) =>
        {
            if (_editor == null) return;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;
            var text = await clipboard.GetTextAsync();
            if (text != null)
                _editor.TextArea.Selection.ReplaceSelectionWithText(text);
        };

        var selectAllItem = new MenuItem { Header = "Selectionner tout (Ctrl+A)" };
        selectAllItem.Click += (_, _) => _editor?.SelectAll();

        var commentItem = new MenuItem { Header = "Commenter/Decommenter (Ctrl+/)" };
        commentItem.Click += (_, _) => _mainViewModel?.ToggleCommentCommand.Execute(null);

        var formatJsonItem = new MenuItem { Header = "Formater JSON" };
        formatJsonItem.Click += OnContextFormatJson;

        var formatXmlItem = new MenuItem { Header = "Formater XML" };
        formatXmlItem.Click += OnContextFormatXml;

        _editor.ContextMenu = new ContextMenu
        {
            Items =
            {
                cutItem, copyItem, pasteItem,
                new Separator(),
                selectAllItem,
                new Separator(),
                commentItem, formatJsonItem, formatXmlItem
            }
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_editor == null)
        {
            _editor = this.FindControl<NotepadEditor>("Editor")
                      ?? this.GetVisualDescendants().OfType<NotepadEditor>().FirstOrDefault();
            if (_editor != null)
                InitializeEditor();
        }

        if (_editor != null && _currentViewModel != null)
            LoadContent(_currentViewModel);

        _editor?.TextArea.Focus();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_editor == null)
        {
            _editor = this.FindControl<NotepadEditor>("Editor");
            if (_editor != null)
                InitializeEditor();
        }

        ConnectToMainViewModel();
    }

    private void ConnectToMainViewModel()
    {
        var window = TopLevel.GetTopLevel(this);

        if (window?.DataContext is ShellViewModel mainVm && _mainViewModel != mainVm)
        {
            if (_mainViewModel != null)
                DisconnectFromMainViewModel();

            _mainViewModel = mainVm;

            // Event bridge handles editor action events
            _eventBridge?.Connect(mainVm);

            // View settings changes (on sub-VM)
            _mainViewModel.Settings.PropertyChanged += OnSettingsPropertyChanged;

            _mainViewModel.FindReplaceViewModel.GetCurrentText = () => _editor?.Text ?? string.Empty;
            _mainViewModel.FindReplaceViewModel.GetCurrentOffset = () => _editor?.CaretOffset ?? 0;
            _mainViewModel.FindReplaceViewModel.NavigateToResult += OnNavigateToResult;
            _mainViewModel.FindReplaceViewModel.ReplaceAllText += OnReplaceAllText;

            ApplyViewSettings();

            // Re-apply content after theme setup
            if (_currentViewModel != null && _editor != null)
            {
                _isUpdatingFromViewModel = true;
                _editor.Text = _currentViewModel.Content;
                _isUpdatingFromViewModel = false;
            }
        }
    }

    private void DisconnectFromMainViewModel()
    {
        if (_mainViewModel == null) return;

        _eventBridge?.Disconnect();
        _mainViewModel.Settings.PropertyChanged -= OnSettingsPropertyChanged;
        _mainViewModel.FindReplaceViewModel.NavigateToResult -= OnNavigateToResult;
        _mainViewModel.FindReplaceViewModel.ReplaceAllText -= OnReplaceAllText;
        _mainViewModel.FindReplaceViewModel.GetCurrentText = null;
        _mainViewModel.FindReplaceViewModel.GetCurrentOffset = null;
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorSettingsViewModel.ShowLineNumbers)
            or nameof(EditorSettingsViewModel.WordWrap)
            or nameof(EditorSettingsViewModel.CurrentTheme)
            or nameof(EditorSettingsViewModel.HighlightCurrentLine)
            or nameof(EditorSettingsViewModel.ShowWhitespace))
        {
            ApplyViewSettings();
        }
        else if (e.PropertyName == nameof(EditorSettingsViewModel.ShowMinimap))
        {
            UpdateMinimapVisibility();
        }
    }

    private void ApplyViewSettings()
    {
        if (_editor == null || _mainViewModel == null) return;

        _editor.ShowLineNumbers = _mainViewModel.Settings.ShowLineNumbers;
        _editor.WordWrap = _mainViewModel.Settings.WordWrap;

        // Set via StyledProperties — NotepadEditor handles the rest
        _editor.HighlightCurrentLine = _mainViewModel.Settings.HighlightCurrentLine;
        _editor.ShowWhitespace = _mainViewModel.Settings.ShowWhitespace;
        _editor.IsDarkTheme = _mainViewModel.Settings.CurrentTheme == "Dark";

        if (_currentViewModel != null)
            _editor.Language = _currentViewModel.Language;

        UpdateMinimapVisibility();
    }

    private void OnNavigateToResult(SearchResult result)
    {
        if (_editor == null) return;
        var offset = _editor.Document.GetOffset(result.Line, result.Column);
        _editor.Select(offset, result.Length);
        _editor.ScrollTo(result.Line, result.Column);
        _editor.TextArea.Focus();
    }

    private void OnReplaceAllText(string newText)
    {
        if (_editor == null) return;
        _isUpdatingFromViewModel = true;
        _editor.Text = newText;
        _isUpdatingFromViewModel = false;
        if (_currentViewModel != null)
            _currentViewModel.Content = newText;
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_editor == null || _mainViewModel == null) return;
        var selection = _editor.TextArea.Selection;
        _mainViewModel.SelectedText = selection.IsEmpty ? null : _editor.SelectedText;
        _editor.UpdateOccurrenceHighlights(selection.IsEmpty ? null : _editor.SelectedText);
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingFromViewModel || _currentViewModel == null || _editor == null) return;

        _isUpdatingFromViewModel = true;
        _currentViewModel.Content = _editor.Text ?? string.Empty;
        _isUpdatingFromViewModel = false;

        _editor.ScheduleRefresh();
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel == null || _editor == null) return;

        var caret = _editor.TextArea.Caret;
        _currentViewModel.CursorLine = caret.Line;
        _currentViewModel.CursorColumn = caret.Column;

        var selection = _editor.TextArea.Selection;
        _currentViewModel.SelectionLength = selection.IsEmpty ? 0 : selection.Length;

        _editor.UpdateBracketHighlight(_editor.CaretOffset);
    }

    private void LoadContent(DocumentTabViewModel viewModel)
    {
        if (_editor == null) return;

        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Grammar-first pattern: apply syntax highlighting before setting content
        _editor.Language = viewModel.Language;

        _isUpdatingFromViewModel = true;
        _editor.Text = viewModel.Content ?? string.Empty;
        _isUpdatingFromViewModel = false;

        // Refresh dev features for the new content
        _editor.SetFileContext(viewModel.FilePath, viewModel.Language);
        _editor.RefreshGitGutter(viewModel.FilePath);
        _editor.RefreshFoldings(viewModel.FilePath, viewModel.Language);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel != null)
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _currentViewModel = DataContext as DocumentTabViewModel;

        if (_currentViewModel != null && _editor != null)
            LoadContent(_currentViewModel);
        else if (DataContext == null && _editor != null)
            _editor.Text = string.Empty;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentTabViewModel.Language) && _currentViewModel != null)
        {
            _editor?.ApplyGrammar(_currentViewModel.Language);
            _editor?.RefreshFoldings(_currentViewModel.FilePath, _currentViewModel.Language);
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.Content) && _currentViewModel != null && _editor != null)
        {
            if (!_isUpdatingFromViewModel && _editor.Text != _currentViewModel.Content)
            {
                _isUpdatingFromViewModel = true;
                _editor.Text = _currentViewModel.Content;
                _isUpdatingFromViewModel = false;
            }
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.CursorLine) && _currentViewModel != null && _editor != null)
        {
            var caret = _editor.TextArea.Caret;
            if (caret.Line != _currentViewModel.CursorLine)
            {
                caret.Line = _currentViewModel.CursorLine;
                caret.Column = 1;
                _editor.ScrollTo(caret.Line, caret.Column);
            }
        }
    }

    private void UpdateMinimapVisibility()
    {
        if (_minimapControl == null || _mainViewModel == null || _editor == null) return;

        if (_mainViewModel.Settings.ShowMinimap)
        {
            _minimapControl.IsVisible = true;
            _minimapControl.AttachEditor(_editor);
        }
        else
        {
            _minimapControl.IsVisible = false;
            _minimapControl.DetachEditor();
        }
    }

    private void OnContextFormatJson(object? sender, RoutedEventArgs e)
    {
        if (_editor == null || _currentViewModel == null || _textTransformService == null) return;
        try
        {
            var formatted = _textTransformService.FormatJson(_editor.Text ?? string.Empty);
            _isUpdatingFromViewModel = true;
            _editor.Text = formatted;
            _isUpdatingFromViewModel = false;
            _currentViewModel.Content = formatted;
        }
        catch { /* invalid JSON */ }
    }

    private void OnContextFormatXml(object? sender, RoutedEventArgs e)
    {
        if (_editor == null || _currentViewModel == null || _textTransformService == null) return;
        try
        {
            var formatted = _textTransformService.FormatXml(_editor.Text ?? string.Empty);
            _isUpdatingFromViewModel = true;
            _editor.Text = formatted;
            _isUpdatingFromViewModel = false;
            _currentViewModel.Content = formatted;
        }
        catch { /* invalid XML */ }
    }
}
