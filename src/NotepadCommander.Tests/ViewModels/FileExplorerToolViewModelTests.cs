using Xunit;
using Avalonia.Platform.Storage;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels.Dialogs;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.ViewModels.Tools;

namespace NotepadCommander.Tests.ViewModels;

public class FileExplorerToolViewModelTests
{
    #region Fakes

    private class FakeFileExplorerService : IFileExplorerService
    {
        public FileTreeNode? LoadDirectory(string path) => new()
        {
            Name = Path.GetFileName(path),
            FullPath = path,
            IsDirectory = true,
            Children = new List<FileTreeNode>
            {
                new() { Name = "file1.cs", FullPath = Path.Combine(path, "file1.cs"), IsDirectory = false },
                new() { Name = "file2.txt", FullPath = Path.Combine(path, "file2.txt"), IsDirectory = false },
                new() { Name = "sub", FullPath = Path.Combine(path, "sub"), IsDirectory = true }
            }
        };
        public void ExpandNode(FileTreeNode node) { }
        public FileTreeNode? FilterTree(FileTreeNode root, string filter) => root;
        public FileTreeNode? FilterTreeByExtensions(FileTreeNode root, IReadOnlySet<string> extensions) => root;
        public void RefreshNode(FileTreeNode node) { }
        public bool CreateFile(string parentPath, string fileName) => true;
        public bool CreateDirectory(string parentPath, string dirName) => true;
        public bool Delete(string path) => true;
        public bool Rename(string oldPath, string newName) => true;
        public (int files, int folders) CountNodes(FileTreeNode node) => (2, 1);
    }

    private class FakeDialogService : IDialogService
    {
        public string? NextInputResult { get; set; }
        public bool NextConfirmResult { get; set; }
        public Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "") =>
            Task.FromResult(NextInputResult);
        public Task<bool> ShowConfirmDialogAsync(string title, string message) =>
            Task.FromResult(NextConfirmResult);
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

    private readonly FakeFileExplorerService _explorerService = new();
    private readonly FakeDialogService _dialogService = new();

    private FileExplorerToolViewModel CreateVm() => new(_explorerService, _dialogService);

    [Fact]
    public void DefaultLocation_IsLeft()
    {
        var vm = CreateVm();
        Assert.Equal(ToolLocation.Left, vm.DefaultLocation);
    }

    [Fact]
    public void LoadFolder_SetsCurrentFolderPath()
    {
        var vm = CreateVm();
        vm.LoadFolder("/test/path");
        Assert.Equal("/test/path", vm.CurrentFolderPath);
    }

    [Fact]
    public void LoadFolder_PopulatesTreeNodes()
    {
        var vm = CreateVm();
        vm.LoadFolder("/test/path");
        Assert.Single(vm.TreeNodes);
        Assert.NotNull(vm.RootNode);
    }

    [Fact]
    public void HasFolderLoaded_TrueAfterLoadFolder()
    {
        var vm = CreateVm();
        Assert.False(vm.HasFolderLoaded);
        vm.LoadFolder("/test/path");
        Assert.True(vm.HasFolderLoaded);
    }

    [Fact]
    public void Refresh_ReloadsTree()
    {
        var vm = CreateVm();
        vm.LoadFolder("/test/path");
        var firstRoot = vm.RootNode;
        vm.Refresh();
        Assert.NotNull(vm.RootNode);
        // New instance created by reload
        Assert.NotSame(firstRoot, vm.RootNode);
    }

    [Fact]
    public void CollapseAll_CollapsesChildNodes()
    {
        var vm = CreateVm();
        vm.LoadFolder("/test/path");
        if (vm.RootNode != null)
        {
            vm.RootNode.IsExpanded = true;
            foreach (var child in vm.RootNode.Children)
                child.IsExpanded = true;
        }
        vm.CollapseAll();
        // Root should remain expanded, children collapsed
        Assert.True(vm.RootNode!.IsExpanded);
        Assert.All(vm.RootNode.Children, child => Assert.False(child.IsExpanded));
    }

    [Fact]
    public void FileOpenRequested_Fires()
    {
        var vm = CreateVm();
        string? openedPath = null;
        vm.FileOpenRequested += path => openedPath = path;
        vm.RequestOpenFile("/test/file.cs");
        Assert.Equal("/test/file.cs", openedPath);
    }

    [Fact]
    public void ToggleExtensionFilter_TogglesExtension()
    {
        var vm = CreateVm();
        vm.LoadFolder("/test/path");
        Assert.True(vm.IsExtensionActive("")); // no filter = "All" active
        vm.ToggleExtensionFilter(".cs");
        Assert.True(vm.IsExtensionActive(".cs"));
        Assert.False(vm.IsExtensionActive(""));
        vm.ToggleExtensionFilter(".cs");
        Assert.True(vm.IsExtensionActive("")); // back to no filter
    }
}
