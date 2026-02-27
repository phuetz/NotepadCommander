using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Error;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.UI.Services;

namespace NotepadCommander.UI.ViewModels;

public partial class TabManagerViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly ISettingsService _settingsService;
    private readonly IErrorHandler _errorHandler;
    private readonly IFileWatcherService _fileWatcherService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<DocumentTabViewModel> Tabs { get; } = new();
    public ObservableCollection<string> RecentFiles { get; } = new();

    [ObservableProperty]
    private DocumentTabViewModel? activeTab;

    [ObservableProperty]
    private string windowTitle = "Notepad Commander";

    [ObservableProperty]
    private DocumentTabViewModel? secondaryTab;

    [ObservableProperty]
    private bool isSplitViewActive;

    public event Action<string?>? ActiveFileChanged;
    public event Action<string, string>? FileChangedExternally;

    public TabManagerViewModel(
        IFileService fileService,
        IRecentFilesService recentFilesService,
        ISettingsService settingsService,
        IErrorHandler errorHandler,
        IFileWatcherService fileWatcherService,
        IDialogService dialogService)
    {
        _fileService = fileService;
        _recentFilesService = recentFilesService;
        _settingsService = settingsService;
        _errorHandler = errorHandler;
        _fileWatcherService = fileWatcherService;
        _dialogService = dialogService;

        foreach (var file in _recentFilesService.RecentFiles)
            RecentFiles.Add(file);

        _fileWatcherService.FileChanged += OnFileChangedExternally;
        _fileWatcherService.FileDeleted += OnFileDeletedExternally;
    }

    public double ZoomLevel { get; set; } = 100;

    partial void OnActiveTabChanged(DocumentTabViewModel? value)
    {
        WindowTitle = value != null
            ? $"{value.Title} - Notepad Commander"
            : "Notepad Commander";
        ActiveFileChanged?.Invoke(value?.FilePath);
    }

    [RelayCommand]
    public void NewDocument()
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
    public async Task OpenFile(object? windowParam)
    {
        try
        {
            var filters = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Tous les fichiers") { Patterns = new[] { "*" } },
                new Avalonia.Platform.Storage.FilePickerFileType("Fichiers texte") { Patterns = new[] { "*.txt", "*.log", "*.md" } },
                new Avalonia.Platform.Storage.FilePickerFileType("Fichiers source") { Patterns = new[] { "*.cs", "*.js", "*.ts", "*.py", "*.java", "*.cpp", "*.c", "*.h" } },
                new Avalonia.Platform.Storage.FilePickerFileType("Fichiers web") { Patterns = new[] { "*.html", "*.htm", "*.css", "*.json", "*.xml", "*.yaml", "*.yml" } },
            };

            var files = await _dialogService.ShowOpenFileDialogAsync("Ouvrir un fichier", filters);
            foreach (var file in files)
            {
                await OpenFilePath(file.Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex, "OpenFile");
        }
    }

    public async Task OpenFilePath(string path)
    {
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
    public async Task SaveFile(object? windowParam)
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
    public async Task SaveFileAs(object? windowParam)
    {
        if (ActiveTab == null) return;

        try
        {
            var filters = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Tous les fichiers") { Patterns = new[] { "*" } },
                new Avalonia.Platform.Storage.FilePickerFileType("Fichiers texte") { Patterns = new[] { "*.txt" } },
            };

            var file = await _dialogService.ShowSaveFileDialogAsync(
                "Enregistrer sous",
                ActiveTab.Document.DisplayName,
                "txt",
                filters);

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
    public async Task SaveAllFiles(object? windowParam)
    {
        foreach (var tab in Tabs.Where(t => t.IsModified))
        {
            ActiveTab = tab;
            await SaveFile(windowParam);
        }
    }

    [RelayCommand]
    public void CloseTab(DocumentTabViewModel? tab)
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
    public void CloseAllTabs()
    {
        foreach (var tab in Tabs.ToList())
            UnwatchClosedFile(tab.FilePath);
        Tabs.Clear();
        NewDocument();
    }

    [RelayCommand]
    public void CloseOtherTabs(DocumentTabViewModel? keepTab)
    {
        keepTab ??= ActiveTab;
        if (keepTab == null) return;
        foreach (var tab in Tabs.Where(t => t != keepTab).ToList())
        {
            UnwatchClosedFile(tab.FilePath);
            Tabs.Remove(tab);
        }
        ActiveTab = keepTab;
    }

    [RelayCommand]
    public void NextTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;
        var index = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(index + 1) % Tabs.Count];
    }

    [RelayCommand]
    public void PreviousTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;
        var index = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(index - 1 + Tabs.Count) % Tabs.Count];
    }

    [RelayCommand]
    public async Task OpenRecentFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try { await OpenFilePath(path); }
        catch (Exception ex) { _errorHandler.HandleError(ex, "OpenRecentFile"); }
    }

    [RelayCommand]
    public void ToggleSplitView()
    {
        IsSplitViewActive = !IsSplitViewActive;
        if (IsSplitViewActive && SecondaryTab == null)
            SecondaryTab = ActiveTab;
    }

    public void MoveTab(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Tabs.Count) return;
        if (toIndex < 0 || toIndex >= Tabs.Count) return;
        if (fromIndex == toIndex) return;
        Tabs.Move(fromIndex, toIndex);
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

    public void UpdateFontSize(double zoomLevel)
    {
        ZoomLevel = zoomLevel;
        if (ActiveTab != null)
            ActiveTab.FontSize = 14 * zoomLevel / 100.0;
    }

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var file in _recentFilesService.RecentFiles)
            RecentFiles.Add(file);
    }

    private void UpdateWindowTitle()
    {
        WindowTitle = ActiveTab != null
            ? $"{ActiveTab.Title} - Notepad Commander"
            : "Notepad Commander";
    }

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
}
