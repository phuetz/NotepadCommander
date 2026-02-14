using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.Error;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.Core.Services.Session;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.Core.Services.AutoComplete;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly ISearchReplaceService _searchReplaceService;
    private readonly ISettingsService _settingsService;
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ISessionService _sessionService;
    private readonly IFileWatcherService _fileWatcherService;
    private readonly ICommentService _commentService;
    private readonly IAutoCompleteService _autoCompleteService;

    public ObservableCollection<DocumentTabViewModel> Tabs { get; } = new();
    public ObservableCollection<string> RecentFiles { get; } = new();

    [ObservableProperty]
    private DocumentTabViewModel? activeTab;

    [ObservableProperty]
    private string windowTitle = "Notepad Commander";

    [ObservableProperty]
    private ToolbarViewModel toolbarViewModel = null!;

    [ObservableProperty]
    private FindReplaceViewModel findReplaceViewModel = null!;

    [ObservableProperty]
    private double zoomLevel = 100;

    // View toggle properties
    [ObservableProperty]
    private bool showLineNumbers = true;

    [ObservableProperty]
    private bool wordWrap;

    [ObservableProperty]
    private string currentTheme = "Light";

    [ObservableProperty]
    private bool isFullScreen;

    [ObservableProperty]
    private bool highlightCurrentLine = true;

    [ObservableProperty]
    private bool showWhitespace;

    [ObservableProperty]
    private bool isSidePanelVisible;

    // LOT 5: Tools properties
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

    // Command palette
    [ObservableProperty]
    private bool isCommandPaletteVisible;

    [ObservableProperty]
    private string commandPaletteQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommandPaletteItem> commandPaletteItems = new();

    // Clipboard history
    private readonly List<string> _clipboardHistory = new();
    public IReadOnlyList<string> ClipboardHistory => _clipboardHistory;

    // Search in files event
    public event Action? ShowSearchInFilesRequested;

    // Editor action events - EditorControl subscribes to these
    public event Action? UndoRequested;
    public event Action? RedoRequested;
    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action<int, int>? SelectionRequested; // offset, length
    public event Action<string>? ToggleCommentRequested; // selected lines text

    // File watcher event
    public event Action<string, string>? FileChangedExternally; // filePath, message

    public MainWindowViewModel(
        IFileService fileService,
        IRecentFilesService recentFilesService,
        ISearchReplaceService searchReplaceService,
        ISettingsService settingsService,
        IErrorHandler errorHandler,
        ILogger<MainWindowViewModel> logger,
        ISessionService sessionService,
        IFileWatcherService fileWatcherService,
        ICommentService commentService,
        IAutoCompleteService autoCompleteService)
    {
        _fileService = fileService;
        _recentFilesService = recentFilesService;
        _searchReplaceService = searchReplaceService;
        _settingsService = settingsService;
        _errorHandler = errorHandler;
        _logger = logger;
        _sessionService = sessionService;
        _fileWatcherService = fileWatcherService;
        _commentService = commentService;
        _autoCompleteService = autoCompleteService;

        ShowLineNumbers = settingsService.Settings.ShowLineNumbers;
        WordWrap = settingsService.Settings.WordWrap;
        CurrentTheme = settingsService.Settings.Theme;
        HighlightCurrentLine = settingsService.Settings.HighlightCurrentLine;
        ShowWhitespace = settingsService.Settings.ShowWhitespace;

        ToolbarViewModel = new ToolbarViewModel(this);
        FindReplaceViewModel = new FindReplaceViewModel(searchReplaceService);
        ZoomLevel = settingsService.Settings.ZoomLevel;

        // Charger les fichiers recents
        foreach (var file in _recentFilesService.RecentFiles)
        {
            RecentFiles.Add(file);
        }

        // Wire up file watcher
        _fileWatcherService.FileChanged += OnFileChangedExternally;
        _fileWatcherService.FileDeleted += OnFileDeletedExternally;

        // Build command palette entries
        BuildCommandPaletteEntries();
    }

    partial void OnActiveTabChanged(DocumentTabViewModel? value)
    {
        WindowTitle = value != null
            ? $"{value.Title} - Notepad Commander"
            : "Notepad Commander";
    }

    partial void OnZoomLevelChanged(double value)
    {
        if (ActiveTab != null)
        {
            ActiveTab.FontSize = 14 * value / 100.0;
        }
        _settingsService.Settings.ZoomLevel = value;
    }

    [RelayCommand]
    private void NewDocument()
    {
        var doc = _fileService.CreateNew();
        var tab = new DocumentTabViewModel(doc)
        {
            FontSize = 14 * ZoomLevel / 100.0
        };
        Tabs.Add(tab);
        ActiveTab = tab;
    }

    [RelayCommand]
    private async Task OpenFile(object? windowParam)
    {
        try
        {
            var window = GetWindow(windowParam);
            if (window == null) return;

            var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Ouvrir un fichier",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Tous les fichiers") { Patterns = new[] { "*" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("Fichiers texte") { Patterns = new[] { "*.txt", "*.log", "*.md" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("Fichiers source") { Patterns = new[] { "*.cs", "*.js", "*.ts", "*.py", "*.java", "*.cpp", "*.c", "*.h" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("Fichiers web") { Patterns = new[] { "*.html", "*.htm", "*.css", "*.json", "*.xml", "*.yaml", "*.yml" } },
                }
            };

            var storage = window.StorageProvider;
            var files = await storage.OpenFilePickerAsync(dialog);

            foreach (var file in files)
            {
                var path = file.Path.LocalPath;
                await OpenFilePath(path);
            }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex, "OpenFile");
        }
    }

    public async Task OpenFilePath(string path)
    {
        // Verifier si deja ouvert
        var existing = Tabs.FirstOrDefault(t =>
            t.FilePath != null && string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            ActiveTab = existing;
            return;
        }

        var doc = await _fileService.OpenAsync(path);
        var tab = new DocumentTabViewModel(doc)
        {
            FontSize = 14 * ZoomLevel / 100.0
        };
        Tabs.Add(tab);
        ActiveTab = tab;

        WatchOpenedFile(path);
        _recentFilesService.AddFile(path);
        RefreshRecentFiles();
    }

    [RelayCommand]
    private async Task SaveFile(object? windowParam)
    {
        if (ActiveTab == null) return;

        try
        {
            if (ActiveTab.Document.IsNew)
            {
                await SaveFileAs(windowParam);
                return;
            }

            await _fileService.SaveAsync(ActiveTab.Document);
            ActiveTab.MarkAsSaved();
            UpdateWindowTitle();
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex, "SaveFile");
        }
    }

    [RelayCommand]
    private async Task SaveFileAs(object? windowParam)
    {
        if (ActiveTab == null) return;

        try
        {
            var window = GetWindow(windowParam);
            if (window == null) return;

            var dialog = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Enregistrer sous",
                SuggestedFileName = ActiveTab.Document.DisplayName,
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Tous les fichiers") { Patterns = new[] { "*" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("Fichiers texte") { Patterns = new[] { "*.txt" } },
                }
            };

            var storage = window.StorageProvider;
            var file = await storage.SaveFilePickerAsync(dialog);

            if (file != null)
            {
                await _fileService.SaveAsAsync(ActiveTab.Document, file.Path.LocalPath);
                ActiveTab.MarkAsSaved();
                _recentFilesService.AddFile(file.Path.LocalPath);
                RefreshRecentFiles();
                UpdateWindowTitle();
            }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex, "SaveFileAs");
        }
    }

    [RelayCommand]
    private async Task SaveAllFiles(object? windowParam)
    {
        foreach (var tab in Tabs.Where(t => t.IsModified))
        {
            ActiveTab = tab;
            await SaveFile(windowParam);
        }
    }

    [RelayCommand]
    private void CloseTab(DocumentTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null) return;

        UnwatchClosedFile(tab.FilePath);
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            NewDocument();
        }
        else if (ActiveTab == tab || ActiveTab == null)
        {
            ActiveTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }
    }

    [RelayCommand]
    private void NextTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;
        var index = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(index + 1) % Tabs.Count];
    }

    [RelayCommand]
    private void PreviousTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;
        var index = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(index - 1 + Tabs.Count) % Tabs.Count];
    }

    [RelayCommand]
    private async Task OpenRecentFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            await OpenFilePath(path);
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex, "OpenRecentFile");
        }
    }

    [RelayCommand]
    private void ShowFind()
    {
        FindReplaceViewModel.Show(false);
    }

    [RelayCommand]
    private void ShowReplace()
    {
        FindReplaceViewModel.Show(true);
    }

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

    // View toggles
    [RelayCommand]
    private void ToggleWordWrap()
    {
        WordWrap = !WordWrap;
        _settingsService.Settings.WordWrap = WordWrap;
    }

    [RelayCommand]
    private void ToggleLineNumbers()
    {
        ShowLineNumbers = !ShowLineNumbers;
        _settingsService.Settings.ShowLineNumbers = ShowLineNumbers;
    }

    [RelayCommand]
    private void SetThemeLight()
    {
        CurrentTheme = "Light";
        _settingsService.Settings.Theme = "Light";
    }

    [RelayCommand]
    private void SetThemeDark()
    {
        CurrentTheme = "Dark";
        _settingsService.Settings.Theme = "Dark";
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 10, 300);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 10, 50);
    }

    [RelayCommand]
    private void ZoomReset()
    {
        ZoomLevel = 100;
    }

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var file in _recentFilesService.RecentFiles)
        {
            RecentFiles.Add(file);
        }
    }

    private void UpdateWindowTitle()
    {
        WindowTitle = ActiveTab != null
            ? $"{ActiveTab.Title} - Notepad Commander"
            : "Notepad Commander";
    }

    // Session save/restore
    public void SaveSession()
    {
        var session = new SessionData
        {
            ActiveTabIndex = ActiveTab != null ? Tabs.IndexOf(ActiveTab) : 0
        };

        foreach (var tab in Tabs)
        {
            session.Tabs.Add(new SessionTab
            {
                FilePath = tab.FilePath,
                UnsavedContent = tab.IsModified ? tab.Content : null,
                CursorLine = tab.CursorLine,
                CursorColumn = tab.CursorColumn
            });
        }

        _sessionService.SaveSession(session);
        _settingsService.Save();
    }

    public async Task RestoreSession()
    {
        var session = _sessionService.LoadSession();
        if (session == null || session.Tabs.Count == 0)
        {
            if (Tabs.Count == 0) NewDocument();
            return;
        }

        // Remove the default empty tab
        Tabs.Clear();

        foreach (var sessionTab in session.Tabs)
        {
            try
            {
                if (sessionTab.FilePath != null && File.Exists(sessionTab.FilePath))
                {
                    var doc = await _fileService.OpenAsync(sessionTab.FilePath);
                    var tab = new DocumentTabViewModel(doc) { FontSize = 14 * ZoomLevel / 100.0 };

                    if (sessionTab.UnsavedContent != null)
                    {
                        tab.Content = sessionTab.UnsavedContent;
                    }

                    tab.CursorLine = sessionTab.CursorLine;
                    tab.CursorColumn = sessionTab.CursorColumn;
                    Tabs.Add(tab);

                    _fileWatcherService.WatchFile(sessionTab.FilePath);
                }
                else if (sessionTab.UnsavedContent != null)
                {
                    var doc = _fileService.CreateNew();
                    var tab = new DocumentTabViewModel(doc)
                    {
                        FontSize = 14 * ZoomLevel / 100.0,
                        Content = sessionTab.UnsavedContent
                    };
                    tab.CursorLine = sessionTab.CursorLine;
                    tab.CursorColumn = sessionTab.CursorColumn;
                    Tabs.Add(tab);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de restaurer l'onglet: {Path}", sessionTab.FilePath);
            }
        }

        if (Tabs.Count == 0)
        {
            NewDocument();
        }
        else
        {
            var idx = Math.Clamp(session.ActiveTabIndex, 0, Tabs.Count - 1);
            ActiveTab = Tabs[idx];
        }
    }

    // File watcher handlers
    private void OnFileChangedExternally(string filePath)
    {
        var tab = Tabs.FirstOrDefault(t =>
            t.FilePath != null && string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (tab == null) return;

        FileChangedExternally?.Invoke(filePath,
            $"Le fichier '{Path.GetFileName(filePath)}' a ete modifie en dehors de l'editeur.\nVoulez-vous le recharger ?");
    }

    private void OnFileDeletedExternally(string filePath)
    {
        var tab = Tabs.FirstOrDefault(t =>
            t.FilePath != null && string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (tab == null) return;

        FileChangedExternally?.Invoke(filePath,
            $"Le fichier '{Path.GetFileName(filePath)}' a ete supprime.\nVoulez-vous le conserver dans l'editeur ?");
    }

    public async Task ReloadFile(string filePath)
    {
        var tab = Tabs.FirstOrDefault(t =>
            t.FilePath != null && string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (tab == null) return;

        try
        {
            var doc = await _fileService.OpenAsync(filePath);
            tab.Content = doc.Content;
            tab.MarkAsSaved();
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex, "ReloadFile");
        }
    }

    // Toggle comment
    [RelayCommand]
    private void ToggleComment()
    {
        if (ActiveTab == null) return;
        ToggleCommentRequested?.Invoke(ActiveTab.Language.ToString());
    }

    // Fullscreen toggle
    [RelayCommand]
    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
    }

    // Current line highlight
    [RelayCommand]
    private void ToggleHighlightCurrentLine()
    {
        HighlightCurrentLine = !HighlightCurrentLine;
        _settingsService.Settings.HighlightCurrentLine = HighlightCurrentLine;
    }

    // Show whitespace
    [RelayCommand]
    private void ToggleShowWhitespace()
    {
        ShowWhitespace = !ShowWhitespace;
        _settingsService.Settings.ShowWhitespace = ShowWhitespace;
    }

    // Side panel
    [RelayCommand]
    private void ToggleSidePanel()
    {
        IsSidePanelVisible = !IsSidePanelVisible;
    }

    // Search in files
    [RelayCommand]
    private void ShowSearchInFiles()
    {
        IsSidePanelVisible = true;
        ShowSearchInFilesRequested?.Invoke();
    }

    // Command palette
    [RelayCommand]
    private void ShowCommandPalette()
    {
        CommandPaletteQuery = string.Empty;
        IsCommandPaletteVisible = true;
        FilterCommandPaletteItems(string.Empty);
    }

    [RelayCommand]
    private void HideCommandPalette()
    {
        IsCommandPaletteVisible = false;
    }

    [RelayCommand]
    private void ExecuteCommandPaletteItem(CommandPaletteItem? item)
    {
        if (item == null) return;
        IsCommandPaletteVisible = false;
        item.Execute();
    }

    partial void OnCommandPaletteQueryChanged(string value)
    {
        FilterCommandPaletteItems(value);
    }

    private List<CommandPaletteItem> _allCommands = new();

    private void BuildCommandPaletteEntries()
    {
        _allCommands = new List<CommandPaletteItem>
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
            new("Onglet suivant", "Ctrl+Tab", () => NextTab()),
            new("Onglet precedent", "Ctrl+Shift+Tab", () => PreviousTab()),
        };
    }

    private void FilterCommandPaletteItems(string query)
    {
        CommandPaletteItems.Clear();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allCommands
            : _allCommands.Where(c =>
                c.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Shortcut.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var item in filtered)
            CommandPaletteItems.Add(item);
    }

    // Clipboard history
    public void AddToClipboardHistory(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _clipboardHistory.Remove(text);
        _clipboardHistory.Insert(0, text);
        if (_clipboardHistory.Count > 20)
            _clipboardHistory.RemoveAt(_clipboardHistory.Count - 1);
    }

    // Auto-complete
    public List<string> GetAutoCompleteSuggestions()
    {
        if (ActiveTab == null) return new();
        return _autoCompleteService.GetSuggestions(
            ActiveTab.Content,
            ActiveTab.CursorColumn, // approximation
            ActiveTab.Language);
    }

    // Comment service access
    public ICommentService CommentService => _commentService;

    // Watch file when opened
    private void WatchOpenedFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            _fileWatcherService.WatchFile(filePath);
    }

    private void UnwatchClosedFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
            _fileWatcherService.UnwatchFile(filePath);
    }

    private static Avalonia.Controls.TopLevel? GetWindow(object? param)
    {
        if (param is Avalonia.Controls.TopLevel topLevel)
            return topLevel;

        // Fallback: KeyBindings pass $self (the KeyBinding), not the Window.
        // Get the main window from the application lifetime.
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }
}

public class CommandPaletteItem
{
    public string Label { get; }
    public string Shortcut { get; }
    private readonly Action _action;

    public CommandPaletteItem(string label, string shortcut, Action action)
    {
        Label = label;
        Shortcut = shortcut;
        _action = action;
    }

    public void Execute() => _action();
}
