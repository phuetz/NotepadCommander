using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.UI.Services;

namespace NotepadCommander.UI.ViewModels.Tools;

public partial class FileExplorerToolViewModel : ToolViewModel
{
    private readonly IFileExplorerService _fileExplorerService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private FileTreeNode? rootNode;

    [ObservableProperty]
    private string? currentFolderPath;

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private string fileCounterText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FileTreeNode> treeNodes = new();

    private readonly HashSet<string> _activeExtensions = new();

    public override ToolLocation DefaultLocation => ToolLocation.Left;

    public string? ActiveFilePath { get; set; }

    public event Action<string>? FileOpenRequested;
    public event Action? TreeRefreshed;

    public FileExplorerToolViewModel(
        IFileExplorerService fileExplorerService,
        IDialogService dialogService)
    {
        _fileExplorerService = fileExplorerService;
        _dialogService = dialogService;
        Title = "Explorateur";
    }

    public bool HasFolderLoaded => CurrentFolderPath != null;

    public void LoadFolder(string path)
    {
        CurrentFolderPath = path;
        RootNode = _fileExplorerService.LoadDirectory(path);
        if (RootNode == null) return;

        FilterText = string.Empty;
        _activeExtensions.Clear();
        ApplyFilters();
        OnPropertyChanged(nameof(HasFolderLoaded));
    }

    [RelayCommand]
    public void Refresh()
    {
        if (CurrentFolderPath == null) return;
        RootNode = _fileExplorerService.LoadDirectory(CurrentFolderPath);
        ApplyFilters();
    }

    [RelayCommand]
    public void CollapseAll()
    {
        if (RootNode == null) return;
        CollapseAllNodes(RootNode);
        RootNode.IsExpanded = true;
        UpdateTreeNodes();
    }

    [RelayCommand]
    public async Task NewFile()
    {
        var parentPath = CurrentFolderPath;
        if (parentPath == null) return;

        var name = await _dialogService.ShowInputDialogAsync("Nouveau fichier", "Nom du fichier :");
        if (string.IsNullOrWhiteSpace(name)) return;

        if (_fileExplorerService.CreateFile(parentPath, name))
        {
            RefreshNodeByPath(parentPath);
            UpdateTreeNodes();
        }
    }

    [RelayCommand]
    public async Task NewFolder()
    {
        var parentPath = CurrentFolderPath;
        if (parentPath == null) return;

        var name = await _dialogService.ShowInputDialogAsync("Nouveau dossier", "Nom du dossier :");
        if (string.IsNullOrWhiteSpace(name)) return;

        if (_fileExplorerService.CreateDirectory(parentPath, name))
        {
            RefreshNodeByPath(parentPath);
            UpdateTreeNodes();
        }
    }

    public async Task NewFileAt(string parentPath)
    {
        var name = await _dialogService.ShowInputDialogAsync("Nouveau fichier", "Nom du fichier :");
        if (string.IsNullOrWhiteSpace(name)) return;

        if (_fileExplorerService.CreateFile(parentPath, name))
        {
            RefreshNodeByPath(parentPath);
            UpdateTreeNodes();
        }
    }

    public async Task NewFolderAt(string parentPath)
    {
        var name = await _dialogService.ShowInputDialogAsync("Nouveau dossier", "Nom du dossier :");
        if (string.IsNullOrWhiteSpace(name)) return;

        if (_fileExplorerService.CreateDirectory(parentPath, name))
        {
            RefreshNodeByPath(parentPath);
            UpdateTreeNodes();
        }
    }

    public bool Rename(string oldPath, string newName)
    {
        if (!_fileExplorerService.Rename(oldPath, newName)) return false;

        var parentPath = Path.GetDirectoryName(oldPath);
        if (parentPath != null)
            RefreshNodeByPath(parentPath);
        UpdateTreeNodes();
        return true;
    }

    public async Task<bool> Delete(FileTreeNode node)
    {
        var confirmed = await _dialogService.ShowConfirmDialogAsync(
            "Supprimer", $"Voulez-vous vraiment supprimer \"{node.Name}\" ?");
        if (!confirmed) return false;

        if (!_fileExplorerService.Delete(node.FullPath)) return false;

        var parentPath = Path.GetDirectoryName(node.FullPath);
        if (parentPath != null)
            RefreshNodeByPath(parentPath);
        UpdateTreeNodes();
        return true;
    }

    public void ExpandNode(FileTreeNode node)
    {
        _fileExplorerService.ExpandNode(node);
        UpdateTreeNodes();
    }

    public void RequestOpenFile(string filePath)
    {
        FileOpenRequested?.Invoke(filePath);
    }

    public void ToggleExtensionFilter(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            _activeExtensions.Clear();
        }
        else
        {
            if (!_activeExtensions.Remove(extension))
                _activeExtensions.Add(extension);
        }
        ApplyFilters();
    }

    public bool IsExtensionActive(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return _activeExtensions.Count == 0;
        return _activeExtensions.Contains(extension);
    }

    public void ApplyFilters()
    {
        if (RootNode == null) return;

        FileTreeNode? filtered = RootNode;

        if (!string.IsNullOrWhiteSpace(FilterText))
            filtered = _fileExplorerService.FilterTree(filtered!, FilterText);

        if (_activeExtensions.Count > 0 && filtered != null)
            filtered = _fileExplorerService.FilterTreeByExtensions(filtered, _activeExtensions);

        TreeNodes.Clear();
        if (filtered != null)
            TreeNodes.Add(filtered);

        UpdateFileCounter(filtered);
        TreeRefreshed?.Invoke();
    }

    partial void OnFilterTextChanged(string value)
    {
        // Debouncing is handled by the View
    }

    private void UpdateTreeNodes()
    {
        if (RootNode == null) return;

        if (!string.IsNullOrWhiteSpace(FilterText) || _activeExtensions.Count > 0)
        {
            ApplyFilters();
            return;
        }

        TreeNodes.Clear();
        TreeNodes.Add(RootNode);
        UpdateFileCounter(RootNode);
        TreeRefreshed?.Invoke();
    }

    private void UpdateFileCounter(FileTreeNode? node)
    {
        if (node == null)
        {
            FileCounterText = string.Empty;
            return;
        }
        var (files, folders) = _fileExplorerService.CountNodes(node);
        FileCounterText = $"{files} fichier{(files > 1 ? "s" : "")}, {folders} dossier{(folders > 1 ? "s" : "")}";
    }

    private void RefreshNodeByPath(string path)
    {
        var node = FindNodeByPath(RootNode, path);
        if (node != null) _fileExplorerService.RefreshNode(node);
    }

    private static void CollapseAllNodes(FileTreeNode node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children)
            CollapseAllNodes(child);
    }

    public static FileTreeNode? FindNodeByPath(FileTreeNode? root, string path)
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
