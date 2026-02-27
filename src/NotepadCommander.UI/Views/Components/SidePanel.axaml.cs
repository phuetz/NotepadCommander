using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace NotepadCommander.UI.Views.Components;

public partial class SidePanel : UserControl
{
    private IFileExplorerService? _fileExplorerService;
    private IDialogService? _dialogService;
    private FileTreeNode? _rootNode;
    private string? _currentFolderPath;
    private enum SidePanelTab { Files, Search, Methods }
    private SidePanelTab _activeTab;

    private DispatcherTimer? _filterDebounce;
    private readonly HashSet<string> _activeExtensions = new();
    private string _currentFilter = string.Empty;
    private string? _activeFilePath;
    private FileTreeNode? _renamingNode;
    private ShellViewModel? _viewModel;

    public SidePanel()
    {
        InitializeComponent();

        var openBtn = this.FindControl<Button>("OpenFolderButton");
        if (openBtn != null)
            openBtn.Click += OnOpenFolderClick;

        var tree = this.FindControl<TreeView>("FileTree");
        if (tree != null)
            tree.DoubleTapped += OnTreeDoubleTapped;

        var filterBox = this.FindControl<TextBox>("FilterTextBox");
        if (filterBox != null)
            filterBox.TextChanged += OnFilterTextChanged;

        AddHandler(DragDrop.DropEvent, OnFolderDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DragDrop.SetAllowDrop(this, true);

        ShowFilesTab();

        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Resolve services once
        if (_fileExplorerService == null)
        {
            try { _fileExplorerService = App.Services.GetRequiredService<IFileExplorerService>(); } catch { }
            try { _dialogService = App.Services.GetService<IDialogService>(); } catch { }
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.DataContext is ShellViewModel vm && _viewModel == null)
        {
            _viewModel = vm;
            vm.ActiveFileChanged += OnActiveFileChanged;
        }
    }

    public void ShowSearchTab()
    {
        _activeTab = SidePanelTab.Search;
        UpdatePanelVisibility();

        var searchPanel = this.FindControl<SearchPanel>("SearchPanelView");
        if (searchPanel != null)
        {
            searchPanel.SetSearchDirectory(_currentFolderPath);
            searchPanel.FocusSearchBox();
        }
    }

    public void ShowMethodsTab()
    {
        _activeTab = SidePanelTab.Methods;
        UpdatePanelVisibility();

        var methodPanel = this.FindControl<MethodSearchPanel>("MethodSearchPanelView");
        if (methodPanel != null)
        {
            methodPanel.SetSearchDirectory(_currentFolderPath);
            methodPanel.FocusInput();
        }
    }

    private void ShowFilesTab()
    {
        _activeTab = SidePanelTab.Files;
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        var filesPanel = this.FindControl<Grid>("FilesPanel");
        var filesHeader = this.FindControl<Border>("FilesHeader");
        var folderPath = this.FindControl<TextBlock>("FolderPathText");
        var searchPanel = this.FindControl<SearchPanel>("SearchPanelView");
        var methodPanel = this.FindControl<MethodSearchPanel>("MethodSearchPanelView");
        var filterBar = this.FindControl<Border>("FilterBar");
        var toolbar = this.FindControl<Border>("FileToolbar");
        var chips = this.FindControl<Border>("ExtensionChips");
        var counter = this.FindControl<Border>("FileCounter");

        var isFiles = _activeTab == SidePanelTab.Files;
        var isSearch = _activeTab == SidePanelTab.Search;
        var isMethods = _activeTab == SidePanelTab.Methods;
        var hasFolderLoaded = _currentFolderPath != null;

        if (filesPanel != null) filesPanel.IsVisible = isFiles;
        if (filesHeader != null) filesHeader.IsVisible = isFiles;
        if (folderPath != null) folderPath.IsVisible = isFiles && hasFolderLoaded;
        if (filterBar != null) filterBar.IsVisible = isFiles && hasFolderLoaded;
        if (toolbar != null) toolbar.IsVisible = isFiles && hasFolderLoaded;
        if (chips != null) chips.IsVisible = isFiles && hasFolderLoaded;
        if (counter != null) counter.IsVisible = isFiles && hasFolderLoaded;
        if (searchPanel != null) searchPanel.IsVisible = isSearch;
        if (methodPanel != null) methodPanel.IsVisible = isMethods;

        UpdateTabHighlights();
    }

    private void UpdateTabHighlights()
    {
        var filesBtn = this.FindControl<Button>("FilesTabButton");
        var searchBtn = this.FindControl<Button>("SearchTabButton");
        var methodsBtn = this.FindControl<Button>("MethodsTabButton");

        if (filesBtn != null)
            filesBtn.FontWeight = _activeTab == SidePanelTab.Files ? FontWeight.Bold : FontWeight.Normal;
        if (searchBtn != null)
            searchBtn.FontWeight = _activeTab == SidePanelTab.Search ? FontWeight.Bold : FontWeight.Normal;
        if (methodsBtn != null)
            methodsBtn.FontWeight = _activeTab == SidePanelTab.Methods ? FontWeight.Bold : FontWeight.Normal;
    }

    private void OnFilesTabClick(object? sender, RoutedEventArgs e) => ShowFilesTab();
    private void OnSearchTabClick(object? sender, RoutedEventArgs e) => ShowSearchTab();
    private void OnMethodsTabClick(object? sender, RoutedEventArgs e) => ShowMethodsTab();

    // --- Filter ---

    private void OnFilterTextChanged(object? sender, TextChangedEventArgs e)
    {
        _filterDebounce?.Stop();
        _filterDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _filterDebounce.Tick += (_, _) =>
        {
            _filterDebounce.Stop();
            ApplyFilters();
        };
        _filterDebounce.Start();

        var filterBox = this.FindControl<TextBox>("FilterTextBox");
        var clearBtn = this.FindControl<Button>("ClearFilterButton");
        if (clearBtn != null && filterBox != null)
            clearBtn.IsVisible = !string.IsNullOrEmpty(filterBox.Text);
    }

    private void OnClearFilterClick(object? sender, RoutedEventArgs e)
    {
        var filterBox = this.FindControl<TextBox>("FilterTextBox");
        if (filterBox != null)
            filterBox.Text = string.Empty;
        _currentFilter = string.Empty;
        _activeExtensions.Clear();
        UpdateChipHighlights();
        ApplyFilters();
    }

    private void OnExtensionChipClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var ext = btn.Tag as string ?? string.Empty;

        if (string.IsNullOrEmpty(ext))
        {
            _activeExtensions.Clear();
        }
        else
        {
            if (_activeExtensions.Contains(ext))
                _activeExtensions.Remove(ext);
            else
                _activeExtensions.Add(ext);
        }

        UpdateChipHighlights();
        ApplyFilters();
    }

    private void UpdateChipHighlights()
    {
        var chips = new[] { "ChipAll", "ChipCs", "ChipJs", "ChipJson", "ChipXml", "ChipMd" };
        foreach (var chipName in chips)
        {
            var btn = this.FindControl<Button>(chipName);
            if (btn == null) continue;
            var ext = btn.Tag as string ?? string.Empty;
            bool isActive = string.IsNullOrEmpty(ext)
                ? _activeExtensions.Count == 0
                : _activeExtensions.Contains(ext);
            btn.FontWeight = isActive ? FontWeight.Bold : FontWeight.Normal;
            btn.Background = isActive
                ? (IBrush?)this.FindResource("PrimaryLightBrush") ?? Brushes.LightGreen
                : Brushes.Transparent;
        }
    }

    private void ApplyFilters()
    {
        if (_rootNode == null || _fileExplorerService == null) return;

        var filterBox = this.FindControl<TextBox>("FilterTextBox");
        _currentFilter = filterBox?.Text ?? string.Empty;

        FileTreeNode? filtered = _rootNode;

        if (!string.IsNullOrWhiteSpace(_currentFilter))
            filtered = _fileExplorerService.FilterTree(filtered!, _currentFilter);

        if (_activeExtensions.Count > 0 && filtered != null)
            filtered = _fileExplorerService.FilterTreeByExtensions(filtered, _activeExtensions);

        var tree = this.FindControl<TreeView>("FileTree");
        if (tree != null && filtered != null)
        {
            tree.ItemsSource = new ObservableCollection<FileTreeNode> { filtered };
            tree.IsVisible = true;
        }

        UpdateFileCounter(filtered);
    }

    // --- Toolbar actions ---

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (_currentFolderPath == null || _fileExplorerService == null) return;
        _rootNode = _fileExplorerService.LoadDirectory(_currentFolderPath);
        ApplyFilters();
        RefreshTree();
    }

    private void OnCollapseAllClick(object? sender, RoutedEventArgs e)
    {
        if (_rootNode == null) return;
        CollapseAllNodes(_rootNode);
        _rootNode.IsExpanded = true;
        RefreshTree();
    }

    private static void CollapseAllNodes(FileTreeNode node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children)
            CollapseAllNodes(child);
    }

    private async void OnNewFileClick(object? sender, RoutedEventArgs e)
    {
        var parentPath = GetSelectedDirectoryPath();
        if (parentPath == null || _fileExplorerService == null) return;

        var name = _dialogService != null
            ? await _dialogService.ShowInputDialogAsync("Nouveau fichier", "Nom du fichier :")
            : null;
        if (string.IsNullOrWhiteSpace(name)) return;

        if (_fileExplorerService.CreateFile(parentPath, name))
        {
            _fileExplorerService.RefreshNode(FindNodeByPath(_rootNode, parentPath) ?? _rootNode!);
            RefreshTree();
        }
    }

    private async void OnNewFolderClick(object? sender, RoutedEventArgs e)
    {
        var parentPath = GetSelectedDirectoryPath();
        if (parentPath == null || _fileExplorerService == null) return;

        var name = _dialogService != null
            ? await _dialogService.ShowInputDialogAsync("Nouveau dossier", "Nom du dossier :")
            : null;
        if (string.IsNullOrWhiteSpace(name)) return;

        if (_fileExplorerService.CreateDirectory(parentPath, name))
        {
            _fileExplorerService.RefreshNode(FindNodeByPath(_rootNode, parentPath) ?? _rootNode!);
            RefreshTree();
        }
    }

    private string? GetSelectedDirectoryPath()
    {
        var tree = this.FindControl<TreeView>("FileTree");
        if (tree?.SelectedItem is FileTreeNode node)
            return node.IsDirectory ? node.FullPath : Path.GetDirectoryName(node.FullPath);
        return _currentFolderPath;
    }

    // --- Context menu ---

    private ContextMenu CreateContextMenu(FileTreeNode node)
    {
        var menu = new ContextMenu();

        if (!node.IsDirectory)
        {
            var openItem = new MenuItem { Header = "Ouvrir" };
            openItem.Click += async (_, _) => await OpenFileInEditor(node.FullPath);
            menu.Items.Add(openItem);
            menu.Items.Add(new Separator());
        }

        var newFileItem = new MenuItem { Header = "Nouveau fichier" };
        newFileItem.Click += async (_, _) =>
        {
            var parent = node.IsDirectory ? node.FullPath : Path.GetDirectoryName(node.FullPath)!;
            var name = _dialogService != null
                ? await _dialogService.ShowInputDialogAsync("Nouveau fichier", "Nom du fichier :")
                : null;
            if (!string.IsNullOrWhiteSpace(name) && _fileExplorerService != null && _fileExplorerService.CreateFile(parent, name))
            {
                var parentNode = FindNodeByPath(_rootNode, parent);
                if (parentNode != null) _fileExplorerService.RefreshNode(parentNode);
                RefreshTree();
            }
        };
        menu.Items.Add(newFileItem);

        var newFolderItem = new MenuItem { Header = "Nouveau dossier" };
        newFolderItem.Click += async (_, _) =>
        {
            var parent = node.IsDirectory ? node.FullPath : Path.GetDirectoryName(node.FullPath)!;
            var name = _dialogService != null
                ? await _dialogService.ShowInputDialogAsync("Nouveau dossier", "Nom du dossier :")
                : null;
            if (!string.IsNullOrWhiteSpace(name) && _fileExplorerService != null && _fileExplorerService.CreateDirectory(parent, name))
            {
                var parentNode = FindNodeByPath(_rootNode, parent);
                if (parentNode != null) _fileExplorerService.RefreshNode(parentNode);
                RefreshTree();
            }
        };
        menu.Items.Add(newFolderItem);

        menu.Items.Add(new Separator());

        var renameItem = new MenuItem { Header = "Renommer" };
        renameItem.Click += (_, _) =>
        {
            var tree = this.FindControl<TreeView>("FileTree");
            if (tree == null) return;
            var treeViewItem = FindTreeViewItemForNode(tree, node);
            if (treeViewItem != null)
                StartInlineRename(node, treeViewItem);
        };
        menu.Items.Add(renameItem);

        var deleteItem = new MenuItem { Header = "Supprimer" };
        deleteItem.Click += async (_, _) =>
        {
            var confirm = _dialogService != null
                && await _dialogService.ShowConfirmDialogAsync(
                    "Supprimer", $"Voulez-vous vraiment supprimer \"{node.Name}\" ?");
            if (confirm && _fileExplorerService != null && _fileExplorerService.Delete(node.FullPath))
            {
                var parentPath = Path.GetDirectoryName(node.FullPath);
                if (parentPath != null)
                {
                    var parentNode = FindNodeByPath(_rootNode, parentPath);
                    if (parentNode != null) _fileExplorerService.RefreshNode(parentNode);
                }
                RefreshTree();
            }
        };
        menu.Items.Add(deleteItem);

        menu.Items.Add(new Separator());

        var copyPathItem = new MenuItem { Header = "Copier le chemin" };
        copyPathItem.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(node.FullPath);
        };
        menu.Items.Add(copyPathItem);

        var openExplorerItem = new MenuItem { Header = "Ouvrir dans l'explorateur" };
        openExplorerItem.Click += (_, _) =>
        {
            var target = node.IsDirectory ? node.FullPath : Path.GetDirectoryName(node.FullPath)!;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
            }
            catch { }
        };
        menu.Items.Add(openExplorerItem);

        return menu;
    }

    // --- Tree events ---

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
        if (_fileExplorerService == null) return;

        _currentFolderPath = path;
        _rootNode = _fileExplorerService.LoadDirectory(path);
        if (_rootNode == null) return;

        var tree = this.FindControl<TreeView>("FileTree");
        var empty = this.FindControl<TextBlock>("EmptyState");
        var pathText = this.FindControl<TextBlock>("FolderPathText");

        if (tree != null)
        {
            tree.ItemsSource = new ObservableCollection<FileTreeNode> { _rootNode };
            tree.IsVisible = true;

            tree.PointerPressed -= OnTreePointerPressed;
            tree.PointerPressed += OnTreePointerPressed;
        }

        if (empty != null)
            empty.IsVisible = false;

        if (pathText != null)
        {
            pathText.Text = path;
            pathText.IsVisible = true;
        }

        _currentFilter = string.Empty;
        _activeExtensions.Clear();
        var filterBox = this.FindControl<TextBox>("FilterTextBox");
        if (filterBox != null) filterBox.Text = string.Empty;
        UpdateChipHighlights();

        UpdatePanelVisibility();
        UpdateFileCounter(_rootNode);

        var searchPanel = this.FindControl<SearchPanel>("SearchPanelView");
        searchPanel?.SetSearchDirectory(path);

        var methodPanel = this.FindControl<MethodSearchPanel>("MethodSearchPanelView");
        methodPanel?.SetSearchDirectory(path);
    }

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        if (sender is not TreeView tree) return;

        var source = e.Source as Visual;
        var treeViewItem = source;
        while (treeViewItem != null && treeViewItem is not TreeViewItem)
            treeViewItem = treeViewItem.GetVisualParent();

        if (treeViewItem is TreeViewItem item && item.DataContext is FileTreeNode node)
        {
            tree.SelectedItem = node;
            var menu = CreateContextMenu(node);
            menu.Open(treeViewItem as Control ?? tree);
            e.Handled = true;
        }
    }

    private async void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView tree) return;
        if (tree.SelectedItem is not FileTreeNode node) return;

        if (node.IsDirectory)
        {
            _fileExplorerService?.ExpandNode(node);
            RefreshTree();
        }
        else
        {
            await OpenFileInEditor(node.FullPath);
        }
    }

    private async Task OpenFileInEditor(string filePath)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is ShellViewModel vm)
        {
            await vm.OpenFilePath(filePath);
        }
    }

    // --- Active file highlight ---

    private void OnActiveFileChanged(string? filePath)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _activeFilePath = filePath;
            HighlightActiveFile();
        });
    }

    private void HighlightActiveFile()
    {
        var tree = this.FindControl<TreeView>("FileTree");
        if (tree == null || _rootNode == null) return;

        if (_activeFilePath != null)
        {
            var node = FindNodeByPath(_rootNode, _activeFilePath);
            if (node != null)
                tree.SelectedItem = node;
        }
    }

    // --- Inline rename ---

    private void StartInlineRename(FileTreeNode node, Control? treeViewItem)
    {
        if (treeViewItem == null) return;
        _renamingNode = node;

        var nameBlock = FindDescendant<TextBlock>(treeViewItem, tb => tb.Text == node.Name);
        if (nameBlock == null) return;

        var parent = nameBlock.GetVisualParent() as StackPanel;
        if (parent == null) return;

        var index = parent.Children.IndexOf(nameBlock);
        nameBlock.IsVisible = false;

        var renameBox = new TextBox
        {
            Text = node.Name,
            FontSize = 12,
            MinWidth = 100,
            Padding = new Thickness(2, 0),
            Margin = new Thickness(0),
            Tag = nameBlock
        };

        parent.Children.Insert(index + 1, renameBox);
        renameBox.Focus();
        renameBox.SelectAll();

        renameBox.KeyDown += OnRenameKeyDown;
        renameBox.LostFocus += OnRenameLostFocus;
    }

    private void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox renameBox) return;

        if (e.Key == Key.Enter)
        {
            CommitRename(renameBox);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelRename(renameBox);
            e.Handled = true;
        }
    }

    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox renameBox)
            CommitRename(renameBox);
    }

    private void CommitRename(TextBox renameBox)
    {
        renameBox.KeyDown -= OnRenameKeyDown;
        renameBox.LostFocus -= OnRenameLostFocus;

        var newName = renameBox.Text?.Trim();
        var node = _renamingNode;
        _renamingNode = null;

        if (node != null && !string.IsNullOrWhiteSpace(newName) && newName != node.Name)
        {
            if (_fileExplorerService != null && _fileExplorerService.Rename(node.FullPath, newName))
            {
                var parentPath = Path.GetDirectoryName(node.FullPath);
                if (parentPath != null)
                {
                    var parentNode = FindNodeByPath(_rootNode, parentPath);
                    if (parentNode != null) _fileExplorerService.RefreshNode(parentNode);
                }
                RefreshTree();
                return;
            }
        }

        RestoreFromRenameBox(renameBox);
    }

    private void CancelRename(TextBox renameBox)
    {
        renameBox.KeyDown -= OnRenameKeyDown;
        renameBox.LostFocus -= OnRenameLostFocus;
        _renamingNode = null;
        RestoreFromRenameBox(renameBox);
    }

    private static void RestoreFromRenameBox(TextBox renameBox)
    {
        if (renameBox.Tag is TextBlock nameBlock)
            nameBlock.IsVisible = true;

        if (renameBox.GetVisualParent() is StackPanel parent)
            parent.Children.Remove(renameBox);
    }

    private static T? FindDescendant<T>(Visual root, Func<T, bool> predicate) where T : Visual
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is T match && predicate(match))
                return match;
            var found = FindDescendant(child, predicate);
            if (found != null) return found;
        }
        return null;
    }

    private static TreeViewItem? FindTreeViewItemForNode(TreeView tree, FileTreeNode node)
    {
        return FindDescendant<TreeViewItem>(tree, item => item.DataContext == node);
    }

    // --- Helpers ---

    private void RefreshTree()
    {
        if (_rootNode == null) return;

        if (!string.IsNullOrWhiteSpace(_currentFilter) || _activeExtensions.Count > 0)
        {
            ApplyFilters();
            return;
        }

        var tree = this.FindControl<TreeView>("FileTree");
        if (tree != null)
            tree.ItemsSource = new ObservableCollection<FileTreeNode> { _rootNode };

        UpdateFileCounter(_rootNode);
    }

    private void UpdateFileCounter(FileTreeNode? node)
    {
        var counterText = this.FindControl<TextBlock>("FileCounterText");
        if (counterText == null || node == null || _fileExplorerService == null) return;

        var (files, folders) = _fileExplorerService.CountNodes(node);
        counterText.Text = $"{files} fichier{(files > 1 ? "s" : "")}, {folders} dossier{(folders > 1 ? "s" : "")}";
    }

    private static FileTreeNode? FindNodeByPath(FileTreeNode? root, string path)
    {
        if (root == null) return null;
        if (string.Equals(root.FullPath, path, StringComparison.OrdinalIgnoreCase))
            return root;
        foreach (var child in root.Children)
        {
            var found = FindNodeByPath(child, path);
            if (found != null) return found;
        }
        return null;
    }
}
