using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.Error;
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

    // Editor action events - EditorControl subscribes to these
    public event Action? UndoRequested;
    public event Action? RedoRequested;
    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action<int, int>? SelectionRequested; // offset, length

    public MainWindowViewModel(
        IFileService fileService,
        IRecentFilesService recentFilesService,
        ISearchReplaceService searchReplaceService,
        ISettingsService settingsService,
        IErrorHandler errorHandler,
        ILogger<MainWindowViewModel> logger)
    {
        _fileService = fileService;
        _recentFilesService = recentFilesService;
        _searchReplaceService = searchReplaceService;
        _settingsService = settingsService;
        _errorHandler = errorHandler;
        _logger = logger;

        ShowLineNumbers = settingsService.Settings.ShowLineNumbers;
        WordWrap = settingsService.Settings.WordWrap;
        CurrentTheme = settingsService.Settings.Theme;

        ToolbarViewModel = new ToolbarViewModel(this);
        FindReplaceViewModel = new FindReplaceViewModel(searchReplaceService);
        ZoomLevel = settingsService.Settings.ZoomLevel;

        // Charger les fichiers recents
        foreach (var file in _recentFilesService.RecentFiles)
        {
            RecentFiles.Add(file);
        }

        // Creer un premier onglet vide
        NewDocument();
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

    private static Avalonia.Controls.TopLevel? GetWindow(object? param)
    {
        if (param is Avalonia.Controls.TopLevel topLevel)
            return topLevel;
        return null;
    }
}
