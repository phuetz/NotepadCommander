using System.Runtime.CompilerServices;
using Xunit;
using Avalonia.Platform.Storage;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels.Dialogs;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.ViewModels.Tools;

namespace NotepadCommander.Tests.ViewModels;

public class SearchToolViewModelTests
{
    #region Fakes

    private class FakeMultiFileSearchService : IMultiFileSearchService
    {
        public List<FileSearchResult> Results { get; } = new();

        public async IAsyncEnumerable<FileSearchResult> SearchInDirectory(
            string directory, string pattern, MultiFileSearchOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var result in Results)
            {
                await Task.CompletedTask;
                yield return result;
            }
        }
    }

    private class FakeSearchReplaceService : ISearchReplaceService
    {
        public IReadOnlyList<SearchResult> FindAll(string text, string pattern, bool useRegex, bool caseSensitive, bool wholeWord) => new List<SearchResult>();
        public SearchResult? FindNext(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord) => null;
        public SearchResult? FindPrevious(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord) => null;
        public string ReplaceAll(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord) => text;
        public (string result, int count) ReplaceAllWithCount(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord) => (text, 0);
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

    #endregion

    private readonly FakeMultiFileSearchService _searchService = new();
    private readonly FakeSearchReplaceService _replaceService = new();
    private readonly FakeDialogService _dialogService = new();

    private SearchToolViewModel CreateVm() => new(_searchService, _replaceService, _dialogService);

    [Fact]
    public void DefaultLocation_IsLeft()
    {
        var vm = CreateVm();
        Assert.Equal(ToolLocation.Left, vm.DefaultLocation);
    }

    [Fact]
    public void ToggleReplace_TogglesShowReplace()
    {
        var vm = CreateVm();
        Assert.False(vm.ShowReplace);
        vm.ToggleReplace();
        Assert.True(vm.ShowReplace);
        vm.ToggleReplace();
        Assert.False(vm.ShowReplace);
    }

    [Fact]
    public async Task Search_WithNoPattern_DoesNothing()
    {
        var vm = CreateVm();
        vm.SearchRootDirectory = "/test";
        vm.SearchPattern = "";
        await vm.Search();
        Assert.Empty(vm.ResultGroups);
    }

    [Fact]
    public async Task Search_WithNoDirectory_DoesNothing()
    {
        var vm = CreateVm();
        vm.SearchPattern = "test";
        vm.SearchRootDirectory = null;
        await vm.Search();
        Assert.Empty(vm.ResultGroups);
    }

    [Fact]
    public async Task Search_WithResults_PopulatesGroups()
    {
        _searchService.Results.Add(new FileSearchResult
        {
            FilePath = "/test/file.cs",
            Line = 10,
            Column = 5,
            LineText = "var test = 1;",
            MatchLength = 4
        });
        _searchService.Results.Add(new FileSearchResult
        {
            FilePath = "/test/file.cs",
            Line = 20,
            Column = 3,
            LineText = "// test comment",
            MatchLength = 4
        });

        var vm = CreateVm();
        vm.SearchRootDirectory = "/test";
        vm.SearchPattern = "test";
        await vm.Search();

        Assert.Single(vm.ResultGroups);
        Assert.Equal(2, vm.ResultGroups[0].MatchCount);
    }

    [Fact]
    public void NavigateToFileRequested_Fires()
    {
        var vm = CreateVm();
        string? navPath = null;
        int navLine = 0;
        vm.NavigateToFileRequested += (path, line) => { navPath = path; navLine = line; };
        vm.RequestNavigateToFile("/test/file.cs", 42);
        Assert.Equal("/test/file.cs", navPath);
        Assert.Equal(42, navLine);
    }
}
