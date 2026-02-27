using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.Core.Services.Session;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.Tests.ViewModels;

public class SessionManagerViewModelTests
{
    #region Fakes

    private class FakeSessionService : ISessionService
    {
        public SessionData? DataToReturn { get; set; }
        public SessionData? LastSaved { get; private set; }

        public SessionData? LoadSession() => DataToReturn;
        public void SaveSession(SessionData session) => LastSaved = session;
        public void ClearSession() { }
    }

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

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings Settings { get; } = new();
        public bool SaveCalled { get; private set; }
        public void Load() { }
        public void Save() => SaveCalled = true;
        public void Reset() { }
    }

    private class FakeRecentFilesService : IRecentFilesService
    {
        public IReadOnlyList<string> RecentFiles => Array.Empty<string>();
        public IReadOnlyList<string> PinnedFiles => Array.Empty<string>();
        public void AddFile(string filePath) { }
        public void RemoveFile(string filePath) { }
        public void PinFile(string filePath) { }
        public void UnpinFile(string filePath) { }
        public void Clear() { }
        public void Load() { }
        public void Save() { }
    }

    private class FakeErrorHandler : Core.Services.Error.IErrorHandler
    {
        public void HandleError(Exception exception, string context = "") { }
        public void HandleWarning(string message, string context = "") { }
    }

    private class FakeFileWatcherService : IFileWatcherService
    {
        public List<string> WatchedFiles { get; } = new();
        public void WatchFile(string filePath) => WatchedFiles.Add(filePath);
        public void UnwatchFile(string filePath) => WatchedFiles.Remove(filePath);
        public void UnwatchAll() => WatchedFiles.Clear();
#pragma warning disable CS0067
        public event Action<string>? FileChanged;
        public event Action<string>? FileDeleted;
#pragma warning restore CS0067
        public void Dispose() { }
    }

    private class FakeDialogService : IDialogService
    {
        public Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "") =>
            Task.FromResult<string?>(null);
        public Task<bool> ShowConfirmDialogAsync(string title, string message) => Task.FromResult(false);
        public Task<IReadOnlyList<Avalonia.Platform.Storage.IStorageFile>> ShowOpenFileDialogAsync(string title, IReadOnlyList<Avalonia.Platform.Storage.FilePickerFileType>? filters = null, bool allowMultiple = true) =>
            Task.FromResult<IReadOnlyList<Avalonia.Platform.Storage.IStorageFile>>(Array.Empty<Avalonia.Platform.Storage.IStorageFile>());
        public Task<Avalonia.Platform.Storage.IStorageFile?> ShowSaveFileDialogAsync(string title, string? suggestedFileName = null, string? defaultExtension = null, IReadOnlyList<Avalonia.Platform.Storage.FilePickerFileType>? filters = null) =>
            Task.FromResult<Avalonia.Platform.Storage.IStorageFile?>(null);
        public Task<IReadOnlyList<Avalonia.Platform.Storage.IStorageFolder>> ShowOpenFolderDialogAsync(string title, bool allowMultiple = false) =>
            Task.FromResult<IReadOnlyList<Avalonia.Platform.Storage.IStorageFolder>>(Array.Empty<Avalonia.Platform.Storage.IStorageFolder>());
        public Task ShowFileChangedDialogAsync(string message, Func<Task> onReload) => Task.CompletedTask;
        public Task<bool> ShowDialogAsync(NotepadCommander.UI.ViewModels.Dialogs.ModalDialogViewModelBase viewModel) =>
            Task.FromResult(false);
        public Task<TResult?> ShowDialogAsync<TResult>(NotepadCommander.UI.ViewModels.Dialogs.ModalDialogViewModelBase<TResult> viewModel) =>
            Task.FromResult<TResult?>(default);
    }

    #endregion

    private readonly FakeSessionService _sessionService = new();
    private readonly FakeFileService _fileService = new();
    private readonly FakeSettingsService _settingsService = new();
    private readonly FakeFileWatcherService _fileWatcherService = new();

    private TabManagerViewModel CreateTabManager() => new(
        _fileService, new FakeRecentFilesService(), _settingsService,
        new FakeErrorHandler(), _fileWatcherService, new FakeDialogService());

    private SessionManagerViewModel CreateVm(TabManagerViewModel tabManager) => new(
        _sessionService, _settingsService, _fileService,
        _fileWatcherService, NullLogger<SessionManagerViewModel>.Instance, tabManager);

    [Fact]
    public void SaveSession_SavesTabState()
    {
        var tabManager = CreateTabManager();
        tabManager.NewDocument();
        tabManager.NewDocument();
        var vm = CreateVm(tabManager);

        vm.SaveSession();

        Assert.NotNull(_sessionService.LastSaved);
        Assert.Equal(2, _sessionService.LastSaved!.Tabs.Count);
        Assert.True(_settingsService.SaveCalled);
    }

    [Fact]
    public void SaveSession_RecordsActiveTabIndex()
    {
        var tabManager = CreateTabManager();
        tabManager.NewDocument();
        tabManager.NewDocument();
        tabManager.NewDocument();
        tabManager.ActiveTab = tabManager.Tabs[1];
        var vm = CreateVm(tabManager);

        vm.SaveSession();

        Assert.Equal(1, _sessionService.LastSaved!.ActiveTabIndex);
    }

    [Fact]
    public async Task RestoreSession_EmptySession_CreatesNewDocument()
    {
        var tabManager = CreateTabManager();
        _sessionService.DataToReturn = null;
        var vm = CreateVm(tabManager);

        await vm.RestoreSession();

        Assert.Single(tabManager.Tabs);
    }

    [Fact]
    public async Task RestoreSession_WithUnsavedContent_RestoresContent()
    {
        var tabManager = CreateTabManager();
        _sessionService.DataToReturn = new SessionData
        {
            Tabs = new List<SessionTab>
            {
                new() { UnsavedContent = "unsaved text", CursorLine = 5 }
            }
        };
        var vm = CreateVm(tabManager);

        await vm.RestoreSession();

        Assert.Single(tabManager.Tabs);
        Assert.Equal("unsaved text", tabManager.Tabs[0].Content);
        Assert.Equal(5, tabManager.Tabs[0].CursorLine);
    }
}
