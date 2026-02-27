using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.AutoComplete;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.UI.Models;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels.Tools;

namespace NotepadCommander.UI.ViewModels;

/// <summary>
/// Shell coordinator — exposes sub-VMs directly, zero facade properties.
/// XAML binds to sub-VM paths: {Binding TabManager.ActiveTab}, {Binding Settings.ZoomLevel}.
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    // Sub-ViewModels — exposed directly for XAML binding
    public TabManagerViewModel TabManager { get; }
    public EditorSettingsViewModel Settings { get; }
    public SessionManagerViewModel Session { get; }
    public CommandPaletteViewModel CommandPalette { get; }
    public ClipboardHistoryViewModel Clipboard { get; }

    // Tool panel VMs
    public FileExplorerToolViewModel FileExplorer { get; }
    public SearchToolViewModel Search { get; }
    public MethodSearchToolViewModel MethodSearch { get; }

    // Services used directly
    private readonly ICommentService _commentService;
    private readonly IAutoCompleteService _autoCompleteService;

    // Editor theme (two-layer: chrome vs editor)
    public EditorThemeService EditorTheme { get; }

    [ObservableProperty]
    private ToolbarViewModel toolbarViewModel = null!;

    [ObservableProperty]
    private FindReplaceViewModel findReplaceViewModel = null!;

    // Tool panel visibility (managed here, not in sub-VMs)
    [ObservableProperty]
    private bool isDiffViewVisible;

    [ObservableProperty]
    private DiffResult? lastDiffResult;

    [ObservableProperty]
    private bool isTerminalVisible;

    [ObservableProperty]
    private bool isMarkdownPreviewVisible;

    [ObservableProperty]
    private bool isSnippetManagerVisible;

    [ObservableProperty]
    private string? calculationResult;

    [ObservableProperty]
    private string? selectedText;

    // Search/method events for SidePanel
    public event Action? ShowSearchInFilesRequested;
    public event Action? ShowMethodSearchRequested;

    // Editor action events — EditorControl subscribes to these
    public event Action? UndoRequested;
    public event Action? RedoRequested;
    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action<int, int>? SelectionRequested;
    public event Action<string>? ToggleCommentRequested;
    public event Action? SelectWordAndHighlightRequested;

    // Line operations events
    public event Action? DuplicateLineRequested;
    public event Action? DeleteLineRequested;
    public event Action<bool>? MoveLineRequested;
    public event Action? GoToMatchingBracketRequested;

    // Forwarded events from sub-VMs
    public event Action<string?>? ActiveFileChanged;
    public event Action<string, string>? FileChangedExternally;

    // Clipboard history event
    public event Action? ShowClipboardHistoryRequested;

    public ShellViewModel(
        TabManagerViewModel tabManager,
        EditorSettingsViewModel settings,
        SessionManagerViewModel session,
        CommandPaletteViewModel commandPalette,
        ClipboardHistoryViewModel clipboard,
        FileExplorerToolViewModel fileExplorer,
        SearchToolViewModel search,
        MethodSearchToolViewModel methodSearch,
        EditorThemeService editorTheme,
        ISearchReplaceService searchReplaceService,
        ICommentService commentService,
        IAutoCompleteService autoCompleteService)
    {
        TabManager = tabManager;
        Settings = settings;
        Session = session;
        CommandPalette = commandPalette;
        Clipboard = clipboard;
        FileExplorer = fileExplorer;
        Search = search;
        MethodSearch = methodSearch;
        EditorTheme = editorTheme;
        _commentService = commentService;
        _autoCompleteService = autoCompleteService;

        ToolbarViewModel = new ToolbarViewModel(this);
        FindReplaceViewModel = new FindReplaceViewModel(searchReplaceService);

        // Sync TabManager.ZoomLevel with Settings.ZoomLevel
        TabManager.ZoomLevel = Settings.ZoomLevel;
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditorSettingsViewModel.ZoomLevel))
                TabManager.UpdateFontSize(Settings.ZoomLevel);
            else if (e.PropertyName == nameof(EditorSettingsViewModel.CurrentTheme))
                EditorTheme.SyncWithChromeTheme(Settings.CurrentTheme);
        };

        // Initial sync
        EditorTheme.SyncWithChromeTheme(Settings.CurrentTheme);

        // Forward events from TabManager
        TabManager.ActiveFileChanged += path => ActiveFileChanged?.Invoke(path);
        TabManager.FileChangedExternally += (path, msg) => FileChangedExternally?.Invoke(path, msg);

        // Forward clipboard show request
        Clipboard.ShowRequested += () => ShowClipboardHistoryRequested?.Invoke();

        // Wire tool panel events
        FileExplorer.FileOpenRequested += async path => await TabManager.OpenFilePath(path);
        Search.NavigateToFileRequested += async (path, line) =>
        {
            await TabManager.OpenFilePath(path);
            if (TabManager.ActiveTab != null)
                TabManager.ActiveTab.CursorLine = line;
        };
        MethodSearch.NavigateToFileRequested += async (path, line) =>
        {
            await TabManager.OpenFilePath(path);
            if (TabManager.ActiveTab != null)
                TabManager.ActiveTab.CursorLine = line;
        };
        MethodSearch.GroupedResultRequested += content =>
        {
            TabManager.NewDocument();
            if (TabManager.ActiveTab != null)
                TabManager.ActiveTab.Content = content;
        };

        // Sync folder path to search VMs
        FileExplorer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileExplorerToolViewModel.CurrentFolderPath))
            {
                Search.SearchRootDirectory = FileExplorer.CurrentFolderPath;
                MethodSearch.SearchDirectory = FileExplorer.CurrentFolderPath;
            }
        };

        BuildCommandPaletteEntries();
    }

    public void RequestSelection(int offset, int length) => SelectionRequested?.Invoke(offset, length);

    // ==========================================
    // Delegating commands
    // ==========================================

    [RelayCommand]
    private void NewDocument() => TabManager.NewDocument();

    [RelayCommand]
    private async Task OpenFile(object? windowParam) => await TabManager.OpenFile(windowParam);

    public Task OpenFilePath(string path) => TabManager.OpenFilePath(path);

    [RelayCommand]
    private async Task SaveFile(object? windowParam) => await TabManager.SaveFile(windowParam);

    [RelayCommand]
    private async Task SaveFileAs(object? windowParam) => await TabManager.SaveFileAs(windowParam);

    [RelayCommand]
    private async Task SaveAllFiles(object? windowParam) => await TabManager.SaveAllFiles(windowParam);

    [RelayCommand]
    private void CloseTab(DocumentTabViewModel? tab) => TabManager.CloseTab(tab);

    [RelayCommand]
    private void CloseAllTabs() => TabManager.CloseAllTabs();

    [RelayCommand]
    private void CloseOtherTabs(DocumentTabViewModel? keepTab) => TabManager.CloseOtherTabs(keepTab);

    [RelayCommand]
    private void NextTab() => TabManager.NextTab();

    [RelayCommand]
    private void PreviousTab() => TabManager.PreviousTab();

    [RelayCommand]
    private async Task OpenRecentFile(string? path) => await TabManager.OpenRecentFile(path);

    [RelayCommand]
    private void ShowFind() => FindReplaceViewModel.Show(false);

    [RelayCommand]
    private void ShowReplace() => FindReplaceViewModel.Show(true);

    // Editor actions - delegate to AvaloniaEdit
    [RelayCommand]
    private void Undo() => UndoRequested?.Invoke();

    [RelayCommand]
    private void Redo() => RedoRequested?.Invoke();

    [RelayCommand]
    private void Cut() => CutRequested?.Invoke();

    [RelayCommand]
    private void Copy() => CopyRequested?.Invoke();

    [RelayCommand]
    private void Paste() => PasteRequested?.Invoke();

    // Settings delegations
    [RelayCommand]
    private void ToggleWordWrap() => Settings.ToggleWordWrap();

    [RelayCommand]
    private void ToggleLineNumbers() => Settings.ToggleLineNumbers();

    [RelayCommand]
    private void SetThemeLight() => Settings.SetThemeLight();

    [RelayCommand]
    private void SetThemeDark() => Settings.SetThemeDark();

    [RelayCommand]
    private void ZoomIn() => Settings.ZoomIn();

    [RelayCommand]
    private void ZoomOut() => Settings.ZoomOut();

    [RelayCommand]
    private void ZoomReset() => Settings.ZoomReset();

    [RelayCommand]
    private void ToggleFullScreen() => Settings.ToggleFullScreen();

    [RelayCommand]
    private void ToggleHighlightCurrentLine() => Settings.ToggleHighlightCurrentLine();

    [RelayCommand]
    private void ToggleShowWhitespace() => Settings.ToggleShowWhitespace();

    [RelayCommand]
    private void ToggleMinimap() => Settings.ToggleMinimap();

    [RelayCommand]
    private void ToggleSidePanel() => Settings.ToggleSidePanel();

    [RelayCommand]
    private void ToggleSplitView() => TabManager.ToggleSplitView();

    // Line operations
    [RelayCommand]
    private void DuplicateLine() => DuplicateLineRequested?.Invoke();

    [RelayCommand]
    private void DeleteLine() => DeleteLineRequested?.Invoke();

    [RelayCommand]
    private void MoveLineUp() => MoveLineRequested?.Invoke(true);

    [RelayCommand]
    private void MoveLineDown() => MoveLineRequested?.Invoke(false);

    [RelayCommand]
    private void GoToMatchingBracket() => GoToMatchingBracketRequested?.Invoke();

    [RelayCommand]
    private void ShowClipboardHistory() => Clipboard.RequestShow();

    [RelayCommand]
    private void SelectWordAndHighlight()
    {
        if (TabManager.ActiveTab == null) return;
        SelectWordAndHighlightRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleComment()
    {
        if (TabManager.ActiveTab == null) return;
        ToggleCommentRequested?.Invoke(TabManager.ActiveTab.Language.ToString());
    }

    // Tool panels
    [RelayCommand]
    private void ToggleMarkdownPreview() => IsMarkdownPreviewVisible = !IsMarkdownPreviewVisible;

    [RelayCommand]
    private void ToggleSnippetManager() => IsSnippetManagerVisible = !IsSnippetManagerVisible;

    [RelayCommand]
    private void ToggleTerminal() => IsTerminalVisible = !IsTerminalVisible;

    [RelayCommand]
    private void ShowSearchInFiles()
    {
        Settings.IsSidePanelVisible = true;
        ShowSearchInFilesRequested?.Invoke();
    }

    [RelayCommand]
    private void ShowMethodSearch()
    {
        Settings.IsSidePanelVisible = true;
        ShowMethodSearchRequested?.Invoke();
    }

    // Command palette
    [RelayCommand]
    private void ShowCommandPalette() => CommandPalette.Show();

    [RelayCommand]
    private void HideCommandPalette() => CommandPalette.Hide();

    [RelayCommand]
    private void ExecuteCommandPaletteItem(CommandPaletteItem? item) => CommandPalette.ExecuteItem(item);

    // Session delegation
    public void SaveSession() => Session.SaveSession();
    public Task RestoreSession() => Session.RestoreSession();
    public Task ReloadFile(string filePath) => TabManager.ReloadFile(filePath);

    // Clipboard history
    public void AddToClipboardHistory(string text) => Clipboard.AddToHistory(text);

    // Comment service access
    public ICommentService CommentService => _commentService;

    // Auto-complete
    public List<string> GetAutoCompleteSuggestions()
    {
        if (TabManager.ActiveTab == null) return new();
        return _autoCompleteService.GetSuggestions(
            TabManager.ActiveTab.Content,
            TabManager.ActiveTab.CursorColumn,
            TabManager.ActiveTab.Language);
    }

    private void BuildCommandPaletteEntries()
    {
        CommandPalette.SetCommands(new List<CommandPaletteItem>
        {
            new("Nouveau document", "Ctrl+N", () => NewDocument()),
            new("Ouvrir un fichier", "Ctrl+O", () => OpenFileCommand.Execute(null)),
            new("Enregistrer", "Ctrl+S", () => SaveFileCommand.Execute(null)),
            new("Enregistrer sous", "Ctrl+Shift+S", () => SaveFileAsCommand.Execute(null)),
            new("Fermer l'onglet", "Ctrl+W", () => CloseTab(null)),
            new("Annuler", "Ctrl+Z", () => Undo()),
            new("Refaire", "Ctrl+Y", () => Redo()),
            new("Couper", "Ctrl+X", () => Cut()),
            new("Copier", "Ctrl+C", () => Copy()),
            new("Coller", "Ctrl+V", () => Paste()),
            new("Rechercher", "Ctrl+F", () => ShowFind()),
            new("Remplacer", "Ctrl+H", () => ShowReplace()),
            new("Commenter/Decommenter", "Ctrl+/", () => ToggleComment()),
            new("Plein ecran", "F11", () => ToggleFullScreen()),
            new("Retour a la ligne", "", () => ToggleWordWrap()),
            new("Numeros de ligne", "", () => ToggleLineNumbers()),
            new("Theme clair", "", () => SetThemeLight()),
            new("Theme sombre", "", () => SetThemeDark()),
            new("Zoom avant", "Ctrl++", () => ZoomIn()),
            new("Zoom arriere", "Ctrl+-", () => ZoomOut()),
            new("Zoom 100%", "Ctrl+0", () => ZoomReset()),
            new("Surligner ligne courante", "", () => ToggleHighlightCurrentLine()),
            new("Afficher espaces", "", () => ToggleShowWhitespace()),
            new("Panneau lateral", "", () => ToggleSidePanel()),
            new("Rechercher dans les fichiers", "Ctrl+Shift+F", () => ShowSearchInFiles()),
            new("Extraction de methodes", "Ctrl+Shift+M", () => ShowMethodSearch()),
            new("Onglet suivant", "Ctrl+Tab", () => NextTab()),
            new("Onglet precedent", "Ctrl+Shift+Tab", () => PreviousTab()),
            new("Selectionner et remplacer les occurrences", "Ctrl+D", () => SelectWordAndHighlight()),
            new("Minimap", "", () => ToggleMinimap()),
            new("Terminal", "Ctrl+`", () => ToggleTerminal()),
            new("Apercu Markdown", "", () => ToggleMarkdownPreview()),
            new("Diviser l'editeur", @"Ctrl+\", () => ToggleSplitView()),
            new("Dupliquer la ligne", "Ctrl+Shift+D", () => DuplicateLine()),
            new("Supprimer la ligne", "Ctrl+Shift+K", () => DeleteLine()),
            new("Deplacer la ligne vers le haut", "Alt+Up", () => MoveLineUp()),
            new("Deplacer la ligne vers le bas", "Alt+Down", () => MoveLineDown()),
            new("Aller au crochet correspondant", "Ctrl+B", () => GoToMatchingBracket()),
            new("Fermer tous les onglets", "", () => CloseAllTabs()),
            new("Fermer les autres onglets", "", () => CloseOtherTabs(null)),
            new("Historique presse-papiers", "Ctrl+Shift+V", () => ShowClipboardHistory()),
            new("Gestionnaire de snippets", "", () => ToggleSnippetManager()),
        });
    }
}
