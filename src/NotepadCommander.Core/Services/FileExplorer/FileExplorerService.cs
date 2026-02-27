using NotepadCommander.Core.Models;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.FileExplorer;

public class FileExplorerService : IFileExplorerService
{
    private readonly ILogger<FileExplorerService> _logger;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", "node_modules", "bin", "obj", "__pycache__", ".vscode"
    };

    public FileExplorerService(ILogger<FileExplorerService> logger)
    {
        _logger = logger;
    }

    public FileTreeNode? LoadDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return null;

            var root = new FileTreeNode
            {
                Name = Path.GetFileName(path),
                FullPath = path,
                IsDirectory = true,
                IsExpanded = true
            };

            PopulateChildren(root);
            return root;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de charger le dossier : {Path}", path);
            return null;
        }
    }

    public void ExpandNode(FileTreeNode node)
    {
        if (!node.IsDirectory || node.Children.Count > 0) return;
        PopulateChildren(node);
        node.IsExpanded = true;
    }

    public FileTreeNode? FilterTree(FileTreeNode root, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return CloneNode(root);
        return FilterNodeByName(root, filter);
    }

    public FileTreeNode? FilterTreeByExtensions(FileTreeNode root, IReadOnlySet<string> extensions)
    {
        if (extensions.Count == 0) return CloneNode(root);
        return FilterNodeByExtensions(root, extensions);
    }

    public void RefreshNode(FileTreeNode node)
    {
        if (!node.IsDirectory) return;
        node.Children.Clear();
        PopulateChildren(node);
    }

    public bool CreateFile(string parentPath, string fileName)
    {
        try
        {
            var fullPath = Path.Combine(parentPath, fileName);
            if (File.Exists(fullPath)) return false;
            File.WriteAllText(fullPath, string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de creer le fichier : {Path}/{Name}", parentPath, fileName);
            return false;
        }
    }

    public bool CreateDirectory(string parentPath, string dirName)
    {
        try
        {
            var fullPath = Path.Combine(parentPath, dirName);
            if (Directory.Exists(fullPath)) return false;
            Directory.CreateDirectory(fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de creer le dossier : {Path}/{Name}", parentPath, dirName);
            return false;
        }
    }

    public bool Delete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de supprimer : {Path}", path);
            return false;
        }
    }

    public bool Rename(string oldPath, string newName)
    {
        try
        {
            var parent = Path.GetDirectoryName(oldPath);
            if (parent == null) return false;
            var newPath = Path.Combine(parent, newName);

            if (File.Exists(oldPath))
            {
                if (File.Exists(newPath)) return false;
                File.Move(oldPath, newPath);
                return true;
            }
            if (Directory.Exists(oldPath))
            {
                if (Directory.Exists(newPath)) return false;
                Directory.Move(oldPath, newPath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de renommer : {Path} -> {NewName}", oldPath, newName);
            return false;
        }
    }

    public (int files, int folders) CountNodes(FileTreeNode node)
    {
        int files = 0, folders = 0;
        CountRecursive(node, ref files, ref folders);
        // Ne pas compter le noeud racine comme dossier
        if (node.IsDirectory) folders--;
        return (files, folders);
    }

    private static void CountRecursive(FileTreeNode node, ref int files, ref int folders)
    {
        if (node.IsDirectory)
        {
            folders++;
            foreach (var child in node.Children)
                CountRecursive(child, ref files, ref folders);
        }
        else
        {
            files++;
        }
    }

    private static FileTreeNode CloneNode(FileTreeNode source)
    {
        var clone = new FileTreeNode
        {
            Name = source.Name,
            FullPath = source.FullPath,
            IsDirectory = source.IsDirectory,
            IsExpanded = source.IsExpanded,
            FileSize = source.FileSize,
            LastModified = source.LastModified
        };
        foreach (var child in source.Children)
            clone.Children.Add(CloneNode(child));
        return clone;
    }

    private static FileTreeNode? FilterNodeByName(FileTreeNode node, string filter)
    {
        if (!node.IsDirectory)
        {
            return node.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                ? CloneNode(node) : null;
        }

        var clone = new FileTreeNode
        {
            Name = node.Name,
            FullPath = node.FullPath,
            IsDirectory = true,
            IsExpanded = true,
            FileSize = node.FileSize,
            LastModified = node.LastModified
        };

        foreach (var child in node.Children)
        {
            var filtered = FilterNodeByName(child, filter);
            if (filtered != null)
                clone.Children.Add(filtered);
        }

        return clone.Children.Count > 0 ? clone : null;
    }

    private static FileTreeNode? FilterNodeByExtensions(FileTreeNode node, IReadOnlySet<string> extensions)
    {
        if (!node.IsDirectory)
        {
            var ext = Path.GetExtension(node.Name).ToLowerInvariant();
            return extensions.Contains(ext) ? CloneNode(node) : null;
        }

        var clone = new FileTreeNode
        {
            Name = node.Name,
            FullPath = node.FullPath,
            IsDirectory = true,
            IsExpanded = true,
            FileSize = node.FileSize,
            LastModified = node.LastModified
        };

        foreach (var child in node.Children)
        {
            var filtered = FilterNodeByExtensions(child, extensions);
            if (filtered != null)
                clone.Children.Add(filtered);
        }

        return clone.Children.Count > 0 ? clone : null;
    }

    private void PopulateChildren(FileTreeNode parent)
    {
        try
        {
            var dirs = Directory.GetDirectories(parent.FullPath)
                .Where(d => !IgnoredDirectories.Contains(Path.GetFileName(d)))
                .OrderBy(d => Path.GetFileName(d));

            foreach (var dir in dirs)
            {
                var info = new DirectoryInfo(dir);
                parent.Children.Add(new FileTreeNode
                {
                    Name = Path.GetFileName(dir),
                    FullPath = dir,
                    IsDirectory = true,
                    LastModified = info.LastWriteTime
                });
            }

            var files = Directory.GetFiles(parent.FullPath)
                .OrderBy(f => Path.GetFileName(f));

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                parent.Children.Add(new FileTreeNode
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    IsDirectory = false,
                    FileSize = info.Length,
                    LastModified = info.LastWriteTime
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignorer les dossiers sans acces
        }
    }
}
