using Xunit;
using Avalonia.Platform.Storage;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Error;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.Tests.ViewModels;

public class TabManagerViewModelTests
{
    #region Fakes

    private class FakeFileService : IFileService
    {
        public TextDocument CreateNew() => new() { Content = "" };
        public Task<TextDocument> OpenAsync(string filePath) =>
            Task.FromResult(new TextDocument { FilePath = filePath, Content = $"content of {filePath}" });
        public Task SaveAsync(TextDocument document) => Task.CompletedTask;
        public Task SaveAsAsync(TextDocument document, string filePath)
        {
            document.FilePath = filePath;
            return Task.CompletedTask;
        }
        public DocumentEncoding DetectEncoding(byte[] data) => DocumentEncoding.Utf8;
        public LineEndingType DetectLineEnding(string content) => LineEndingType.CrLf;
        public SupportedLanguage DetectLanguage(string filePath) => SupportedLanguage.PlainText;
        public System.Text.Encoding GetDotNetEncoding(DocumentEncoding encoding) => System.Text.Encoding.UTF8;
    }

    private class FakeRecentFilesService : IRecentFilesService
    {
        private readonly List<string> _files = new();
        public IReadOnlyList<string> RecentFiles => _files;
        public IReadOnlyList<string> PinnedFiles => Array.Empty<string>();
        public void AddFile(string filePath) { _files.Remove(filePath); _files.Insert(0, filePath); }
        public void RemoveFile(string filePath) => _files.Remove(filePath);
        public void PinFile(string filePath) { }
        public void UnpinFile(string filePath) { }
        public void Clear() => _files.Clear();
        public void Load() { }
        public void Save() { }
    }

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings Settings { get; } = new();
        public void Load() { }
        public void Save() { }
        public void Reset() { }
    }

    private class FakeErrorHandler : IErrorHandler
    {
        public List<string> Errors { get; } = new();
        public void HandleError(Exception exception, string context = "") => Errors.Add($"{context}: {exception.Message}");
        public void HandleWarning(string message, string context = "") { }
    }

    private class FakeFileWatcherService : IFileWatcherService
    {
        public List<string> WatchedFiles { get; } = new();
        public void WatchFile(string filePath) => WatchedFiles.Add(filePath);
        public void UnwatchFile(string filePath) => WatchedFiles.Remove(filePath);
        public void UnwatchAll() => WatchedFiles.Clear();
        public event Action<string>? FileChanged;
        public event Action<string>? FileDeleted;
        public void Dispose() { }

        public void SimulateFileChanged(string path) => FileChanged?.Invoke(path);
        public void SimulateFileDeleted(string path) => FileDeleted?.Invoke(path);
    }

    private class FakeDialogService : IDialogService
    {
        public Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "") =>
            Task.FromResult<string?>(null);
        public Task<bool> ShowConfirmDialogAsync(string title, string message) =>
            Task.FromResult(false);
        public Task<IReadOnlyList<IStorageFile>> ShowOpenFileDialogAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null, bool allowMultiple = true) =>
            Task.FromResult<IReadOnlyList<IStorageFile>>(Array.Empty<IStorageFile>());
        public Task<IStorageFile?> ShowSaveFileDialogAsync(string title, string? suggestedFileName = null, string? defaultExtension = null, IReadOnlyList<FilePickerFileType>? filters = null) =>
            Task.FromResult<IStorageFile?>(null);
        public Task<IReadOnlyList<IStorageFolder>> ShowOpenFolderDialogAsync(string title, bool allowMultiple = false) =>
            Task.FromResult<IReadOnlyList<IStorageFolder>>(Array.Empty<IStorageFolder>());
        public Task ShowFileChangedDialogAsync(string message, Func<Task> onReload) =>
            Task.CompletedTask;
        public Task<bool> ShowDialogAsync(NotepadCommander.UI.ViewModels.Dialogs.ModalDialogViewModelBase viewModel) =>
            Task.FromResult(false);
        public Task<TResult?> ShowDialogAsync<TResult>(NotepadCommander.UI.ViewModels.Dialogs.ModalDialogViewModelBase<TResult> viewModel) =>
            Task.FromResult<TResult?>(default);
    }

    #endregion

    private readonly FakeFileService _fileService = new();
    private readonly FakeRecentFilesService _recentFilesService = new();
    private readonly FakeSettingsService _settingsService = new();
    private readonly FakeErrorHandler _errorHandler = new();
    private readonly FakeFileWatcherService _fileWatcherService = new();
    private readonly FakeDialogService _dialogService = new();

    private TabManagerViewModel CreateVm() => new(
        _fileService, _recentFilesService, _settingsService,
        _errorHandler, _fileWatcherService, _dialogService);

    [Fact]
    public void NewDocument_AddsTabAndSetsActive()
    {
        var vm = CreateVm();

        vm.NewDocument();

        Assert.Single(vm.Tabs);
        Assert.NotNull(vm.ActiveTab);
        Assert.Equal(vm.Tabs[0], vm.ActiveTab);
    }

    [Fact]
    public void NewDocument_MultipleTabs()
    {
        var vm = CreateVm();

        vm.NewDocument();
        vm.NewDocument();

        Assert.Equal(2, vm.Tabs.Count);
        Assert.Equal(vm.Tabs[1], vm.ActiveTab);
    }

    [Fact]
    public async Task OpenFilePath_AddsTabAndSetsActive()
    {
        var vm = CreateVm();

        await vm.OpenFilePath("test.cs");

        Assert.Single(vm.Tabs);
        Assert.NotNull(vm.ActiveTab);
        Assert.Equal("test.cs", vm.ActiveTab.FilePath);
    }

    [Fact]
    public async Task OpenFilePath_SameFile_ActivatesExisting()
    {
        var vm = CreateVm();

        await vm.OpenFilePath("test.cs");
        vm.NewDocument();
        await vm.OpenFilePath("test.cs");

        Assert.Equal(2, vm.Tabs.Count);
        Assert.Equal("test.cs", vm.ActiveTab!.FilePath);
    }

    [Fact]
    public async Task OpenFilePath_WatchesFile()
    {
        var vm = CreateVm();
        // WatchOpenedFile checks File.Exists, so use a real temp file
        var tempFile = Path.GetTempFileName();
        try
        {
            await vm.OpenFilePath(tempFile);
            Assert.Contains(tempFile, _fileWatcherService.WatchedFiles);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public async Task OpenFilePath_AddsToRecentFiles()
    {
        var vm = CreateVm();

        await vm.OpenFilePath("test.cs");

        Assert.Contains("test.cs", _recentFilesService.RecentFiles);
    }

    [Fact]
    public void CloseTab_RemovesTab()
    {
        var vm = CreateVm();
        vm.NewDocument();
        vm.NewDocument();
        var tab = vm.Tabs[0];

        vm.CloseTab(tab);

        Assert.Single(vm.Tabs);
        Assert.DoesNotContain(tab, vm.Tabs);
    }

    [Fact]
    public void CloseTab_LastTab_CreatesNewDocument()
    {
        var vm = CreateVm();
        vm.NewDocument();
        var tab = vm.Tabs[0];

        vm.CloseTab(tab);

        Assert.Single(vm.Tabs);
        Assert.NotNull(vm.ActiveTab);
    }

    [Fact]
    public void CloseAllTabs_ClearsAndCreatesNew()
    {
        var vm = CreateVm();
        vm.NewDocument();
        vm.NewDocument();
        vm.NewDocument();

        vm.CloseAllTabs();

        Assert.Single(vm.Tabs);
    }

    [Fact]
    public void CloseOtherTabs_KeepsSpecifiedTab()
    {
        var vm = CreateVm();
        vm.NewDocument();
        vm.NewDocument();
        vm.NewDocument();
        var keepTab = vm.Tabs[1];

        vm.CloseOtherTabs(keepTab);

        Assert.Single(vm.Tabs);
        Assert.Equal(keepTab, vm.Tabs[0]);
        Assert.Equal(keepTab, vm.ActiveTab);
    }

    [Fact]
    public void NextTab_CyclesForward()
    {
        var vm = CreateVm();
        vm.NewDocument();
        vm.NewDocument();
        vm.NewDocument();
        vm.ActiveTab = vm.Tabs[0];

        vm.NextTab();
        Assert.Equal(vm.Tabs[1], vm.ActiveTab);

        vm.NextTab();
        Assert.Equal(vm.Tabs[2], vm.ActiveTab);

        vm.NextTab();
        Assert.Equal(vm.Tabs[0], vm.ActiveTab);
    }

    [Fact]
    public void PreviousTab_CyclesBackward()
    {
        var vm = CreateVm();
        vm.NewDocument();
        vm.NewDocument();
        vm.NewDocument();
        vm.ActiveTab = vm.Tabs[0];

        vm.PreviousTab();
        Assert.Equal(vm.Tabs[2], vm.ActiveTab);
    }

    [Fact]
    public void ActiveTabChanged_UpdatesWindowTitle()
    {
        var vm = CreateVm();

        vm.NewDocument();

        Assert.Contains("Notepad Commander", vm.WindowTitle);
    }

    [Fact]
    public void ActiveTabChanged_FiresActiveFileChanged()
    {
        var vm = CreateVm();
        string? firedPath = null;
        vm.ActiveFileChanged += path => firedPath = path;

        vm.NewDocument();

        Assert.Null(firedPath); // new document has no FilePath
    }

    [Fact]
    public void ToggleSplitView_Toggles()
    {
        var vm = CreateVm();
        vm.NewDocument();

        vm.ToggleSplitView();
        Assert.True(vm.IsSplitViewActive);
        Assert.Equal(vm.ActiveTab, vm.SecondaryTab);

        vm.ToggleSplitView();
        Assert.False(vm.IsSplitViewActive);
    }

    [Fact]
    public void UpdateFontSize_UpdatesActiveTabFontSize()
    {
        var vm = CreateVm();
        vm.NewDocument();

        vm.UpdateFontSize(200);

        Assert.Equal(200, vm.ZoomLevel);
        Assert.Equal(14 * 200 / 100.0, vm.ActiveTab!.FontSize);
    }

    [Fact]
    public async Task FileChangedExternally_FiresEvent()
    {
        var vm = CreateVm();
        string? eventPath = null;
        vm.FileChangedExternally += (path, _) => eventPath = path;

        // Open a file, then simulate external change
        await vm.OpenFilePath("test.cs");

        _fileWatcherService.SimulateFileChanged("test.cs");

        Assert.Equal("test.cs", eventPath);
    }
}
