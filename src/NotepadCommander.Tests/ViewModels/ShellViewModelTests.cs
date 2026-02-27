using System.Runtime.CompilerServices;
using Xunit;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.AutoComplete;
using NotepadCommander.Core.Services.Error;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.Core.Services.MethodExtractor;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.Core.Services.Session;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.ViewModels.Dialogs;
using NotepadCommander.UI.ViewModels.Tools;

namespace NotepadCommander.Tests.ViewModels;

public class ShellViewModelTests
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
        public Task<bool> ShowDialogAsync(ModalDialogViewModelBase viewModel) =>
            Task.FromResult(false);
        public Task<TResult?> ShowDialogAsync<TResult>(ModalDialogViewModelBase<TResult> viewModel) =>
            Task.FromResult<TResult?>(default);
    }

    private class FakeSessionService : ISessionService
    {
        public SessionData? LoadSession() => null;
        public void SaveSession(SessionData session) { }
        public void ClearSession() { }
    }

    private class FakeSearchReplaceService : ISearchReplaceService
    {
        public IReadOnlyList<SearchResult> FindAll(string text, string pattern, bool useRegex, bool caseSensitive, bool wholeWord) => new List<SearchResult>();
        public SearchResult? FindNext(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord) => null;
        public SearchResult? FindPrevious(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord) => null;
        public string ReplaceAll(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord) => text;
        public (string result, int count) ReplaceAllWithCount(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord) => (text, 0);
    }

    private class FakeCommentService : ICommentService
    {
        public string ToggleComment(string text, SupportedLanguage language) => text;
        public string CommentLines(string text, SupportedLanguage language) => text;
        public string UncommentLines(string text, SupportedLanguage language) => text;
        public string? GetCommentPrefix(SupportedLanguage language) => "//";
    }

    private class FakeAutoCompleteService : IAutoCompleteService
    {
        public List<string> GetSuggestions(string text, int cursorPosition, SupportedLanguage language, int maxResults = 20) => new();
    }

    private class FakeFileExplorerService : IFileExplorerService
    {
        public FileTreeNode? LoadDirectory(string path) => new() { Name = "root", FullPath = path, IsDirectory = true };
        public void ExpandNode(FileTreeNode node) { }
        public FileTreeNode? FilterTree(FileTreeNode root, string filter) => root;
        public FileTreeNode? FilterTreeByExtensions(FileTreeNode root, IReadOnlySet<string> extensions) => root;
        public void RefreshNode(FileTreeNode node) { }
        public bool CreateFile(string parentPath, string fileName) => true;
        public bool CreateDirectory(string parentPath, string dirName) => true;
        public bool Delete(string path) => true;
        public bool Rename(string oldPath, string newName) => true;
        public (int files, int folders) CountNodes(FileTreeNode node) => (0, 0);
    }

    private class FakeMultiFileSearchService : IMultiFileSearchService
    {
        public async IAsyncEnumerable<FileSearchResult> SearchInDirectory(string directory, string pattern, MultiFileSearchOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private class FakeMethodExtractorService : IMethodExtractorService
    {
        public async IAsyncEnumerable<Core.Models.MethodInfo> ExtractMethods(string directory, string[] methodNames, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    #endregion

    private readonly FakeFileService _fileService = new();
    private readonly FakeRecentFilesService _recentFilesService = new();
    private readonly FakeSettingsService _settingsService = new();
    private readonly FakeErrorHandler _errorHandler = new();
    private readonly FakeFileWatcherService _fileWatcherService = new();
    private readonly FakeDialogService _dialogService = new();
    private readonly FakeSearchReplaceService _searchReplaceService = new();
    private readonly FakeCommentService _commentService = new();
    private readonly FakeAutoCompleteService _autoCompleteService = new();

    private ShellViewModel CreateVm()
    {
        var tabManager = new TabManagerViewModel(
            _fileService, _recentFilesService, _settingsService,
            _errorHandler, _fileWatcherService, _dialogService);
        var settings = new EditorSettingsViewModel(_settingsService);
        var session = new SessionManagerViewModel(
            new FakeSessionService(), _settingsService, _fileService,
            _fileWatcherService, NullLogger<SessionManagerViewModel>.Instance, tabManager);
        var commandPalette = new CommandPaletteViewModel();
        var clipboard = new ClipboardHistoryViewModel();
        var fileExplorer = new FileExplorerToolViewModel(
            new FakeFileExplorerService(), _dialogService);
        var search = new SearchToolViewModel(
            new FakeMultiFileSearchService(), _searchReplaceService, _dialogService);
        var methodSearch = new MethodSearchToolViewModel(new FakeMethodExtractorService());
        var editorTheme = new EditorThemeService();

        return new ShellViewModel(
            tabManager, settings, session, commandPalette, clipboard,
            fileExplorer, search, methodSearch, editorTheme,
            _searchReplaceService, _commentService, _autoCompleteService);
    }

    [Fact]
    public void NewDocumentCommand_CreatesTab()
    {
        var vm = CreateVm();
        vm.NewDocumentCommand.Execute(null);
        Assert.Single(vm.TabManager.Tabs);
        Assert.NotNull(vm.TabManager.ActiveTab);
    }

    [Fact]
    public void ToggleTerminal_TogglesVisibility()
    {
        var vm = CreateVm();
        Assert.False(vm.IsTerminalVisible);
        vm.ToggleTerminalCommand.Execute(null);
        Assert.True(vm.IsTerminalVisible);
        vm.ToggleTerminalCommand.Execute(null);
        Assert.False(vm.IsTerminalVisible);
    }

    [Fact]
    public void ToggleMarkdownPreview_TogglesVisibility()
    {
        var vm = CreateVm();
        Assert.False(vm.IsMarkdownPreviewVisible);
        vm.ToggleMarkdownPreviewCommand.Execute(null);
        Assert.True(vm.IsMarkdownPreviewVisible);
    }

    [Fact]
    public void ToggleSnippetManager_TogglesVisibility()
    {
        var vm = CreateVm();
        Assert.False(vm.IsSnippetManagerVisible);
        vm.ToggleSnippetManagerCommand.Execute(null);
        Assert.True(vm.IsSnippetManagerVisible);
    }

    [Fact]
    public void AddToClipboardHistory_DelegatesToClipboard()
    {
        var vm = CreateVm();
        vm.AddToClipboardHistory("test");
        Assert.Contains("test", vm.Clipboard.History);
    }

    [Fact]
    public void EditorTheme_SyncsWithSettingsTheme()
    {
        var vm = CreateVm();
        vm.Settings.SetThemeDark();
        Assert.True(vm.EditorTheme.IsDarkEditorTheme);
        vm.Settings.SetThemeLight();
        Assert.False(vm.EditorTheme.IsDarkEditorTheme);
    }

    [Fact]
    public void ShowSearchInFiles_MakesSidePanelVisible()
    {
        var vm = CreateVm();
        vm.Settings.IsSidePanelVisible = false;
        vm.ShowSearchInFilesCommand.Execute(null);
        Assert.True(vm.Settings.IsSidePanelVisible);
    }
}
