using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.UI.ViewModels;
using System.Collections.ObjectModel;

namespace NotepadCommander.UI.Views.Components;

public partial class SidePanel : UserControl
{
    private readonly IFileExplorerService _fileExplorerService;
    private FileTreeNode? _rootNode;

    public SidePanel()
    {
        InitializeComponent();
        _fileExplorerService = App.Services.GetRequiredService<IFileExplorerService>();

        var openBtn = this.FindControl<Button>("OpenFolderButton");
        if (openBtn != null)
            openBtn.Click += OnOpenFolderClick;

        var tree = this.FindControl<TreeView>("FileTree");
        if (tree != null)
            tree.DoubleTapped += OnTreeDoubleTapped;

        // Support folder drag & drop
        AddHandler(DragDrop.DropEvent, OnFolderDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DragDrop.SetAllowDrop(this, true);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnFolderDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var files = e.Data.GetFiles();
        if (files == null) return;

        foreach (var item in files)
        {
            var path = item.Path.LocalPath;
            if (Directory.Exists(path))
            {
                LoadFolder(path);
                break;
            }
        }
    }

    private async void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Ouvrir un dossier", AllowMultiple = false });

        if (folders.Count > 0)
        {
            LoadFolder(folders[0].Path.LocalPath);
        }
    }

    private void LoadFolder(string path)
    {
        _rootNode = _fileExplorerService.LoadDirectory(path);
        if (_rootNode == null) return;

        var tree = this.FindControl<TreeView>("FileTree");
        var empty = this.FindControl<TextBlock>("EmptyState");
        var pathText = this.FindControl<TextBlock>("FolderPathText");

        if (tree != null)
        {
            tree.ItemsSource = new ObservableCollection<FileTreeNode> { _rootNode };
            tree.IsVisible = true;
        }

        if (empty != null)
            empty.IsVisible = false;

        if (pathText != null)
        {
            pathText.Text = path;
            pathText.IsVisible = true;
        }
    }

    private async void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView tree) return;
        if (tree.SelectedItem is not FileTreeNode node) return;

        if (node.IsDirectory)
        {
            _fileExplorerService.ExpandNode(node);
            // Refresh the tree
            if (_rootNode != null)
                tree.ItemsSource = new ObservableCollection<FileTreeNode> { _rootNode };
        }
        else
        {
            // Open file in editor
            var window = TopLevel.GetTopLevel(this);
            if (window?.DataContext is MainWindowViewModel vm)
            {
                await vm.OpenFilePath(node.FullPath);
            }
        }
    }
}
