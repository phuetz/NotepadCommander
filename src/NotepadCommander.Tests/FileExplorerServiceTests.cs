using Xunit;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NotepadCommander.Tests;

public class FileExplorerServiceTests : IDisposable
{
    private readonly FileExplorerService _service;
    private readonly string _tempDir;

    public FileExplorerServiceTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<FileExplorerService>();
        _service = new FileExplorerService(logger);
        _tempDir = Path.Combine(Path.GetTempPath(), "NotepadCmdr_FE_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void LoadDirectory_ReturnsNull_ForNonExistent()
    {
        var result = _service.LoadDirectory(@"Z:\nonexistent\path");
        Assert.Null(result);
    }

    [Fact]
    public void LoadDirectory_ReturnsRootNode()
    {
        var root = _service.LoadDirectory(_tempDir);
        Assert.NotNull(root);
        Assert.True(root.IsDirectory);
        Assert.True(root.IsExpanded);
        Assert.Equal(Path.GetFileName(_tempDir), root.Name);
    }

    [Fact]
    public void LoadDirectory_ListsFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "file2.cs"), "b");

        var root = _service.LoadDirectory(_tempDir);
        Assert.NotNull(root);
        Assert.Equal(2, root.Children.Count);
        Assert.All(root.Children, c => Assert.False(c.IsDirectory));
    }

    [Fact]
    public void LoadDirectory_ListsSubdirectories()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "a");

        var root = _service.LoadDirectory(_tempDir);
        Assert.NotNull(root);
        // Directories come first, then files
        Assert.True(root.Children[0].IsDirectory);
        Assert.Equal("subdir", root.Children[0].Name);
        Assert.False(root.Children[1].IsDirectory);
    }

    [Fact]
    public void LoadDirectory_IgnoresGitDirectory()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "src"));

        var root = _service.LoadDirectory(_tempDir);
        Assert.NotNull(root);
        Assert.Single(root.Children);
        Assert.Equal("src", root.Children[0].Name);
    }

    [Fact]
    public void LoadDirectory_IgnoresNodeModules()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "node_modules"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "lib"));

        var root = _service.LoadDirectory(_tempDir);
        Assert.NotNull(root);
        Assert.Single(root.Children);
        Assert.Equal("lib", root.Children[0].Name);
    }

    [Fact]
    public void ExpandNode_PopulatesChildren()
    {
        var subdir = Path.Combine(_tempDir, "expand_test");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(subdir, "inner.txt"), "content");

        var node = new FileTreeNode
        {
            Name = "expand_test",
            FullPath = subdir,
            IsDirectory = true
        };

        Assert.Empty(node.Children);
        _service.ExpandNode(node);
        Assert.Single(node.Children);
        Assert.Equal("inner.txt", node.Children[0].Name);
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void ExpandNode_DoesNothing_ForFile()
    {
        var file = Path.Combine(_tempDir, "afile.txt");
        File.WriteAllText(file, "content");

        var node = new FileTreeNode
        {
            Name = "afile.txt",
            FullPath = file,
            IsDirectory = false
        };

        _service.ExpandNode(node);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void FileTreeNode_Icon_Directory()
    {
        var node = new FileTreeNode { IsDirectory = true, IsExpanded = false };
        Assert.Equal("📁", node.Icon);
        node.IsExpanded = true;
        Assert.Equal("📂", node.Icon);
    }

    [Fact]
    public void FileTreeNode_Icon_FileExtensions()
    {
        Assert.Equal("🟢", new FileTreeNode { Name = "Program.cs" }.Icon);
        Assert.Equal("🟡", new FileTreeNode { Name = "app.js" }.Icon);
        Assert.Equal("🔵", new FileTreeNode { Name = "script.py" }.Icon);
        Assert.Equal("📋", new FileTreeNode { Name = "data.json" }.Icon);
        Assert.Equal("📝", new FileTreeNode { Name = "README.md" }.Icon);
        Assert.Equal("🌐", new FileTreeNode { Name = "index.html" }.Icon);
        Assert.Equal("🎨", new FileTreeNode { Name = "styles.css" }.Icon);
        Assert.Equal("📄", new FileTreeNode { Name = "file.xyz" }.Icon);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
