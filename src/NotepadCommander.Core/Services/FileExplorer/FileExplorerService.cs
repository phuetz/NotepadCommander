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

    private void PopulateChildren(FileTreeNode parent)
    {
        try
        {
            var dirs = Directory.GetDirectories(parent.FullPath)
                .Where(d => !IgnoredDirectories.Contains(Path.GetFileName(d)))
                .OrderBy(d => Path.GetFileName(d));

            foreach (var dir in dirs)
            {
                parent.Children.Add(new FileTreeNode
                {
                    Name = Path.GetFileName(dir),
                    FullPath = dir,
                    IsDirectory = true
                });
            }

            var files = Directory.GetFiles(parent.FullPath)
                .OrderBy(f => Path.GetFileName(f));

            foreach (var file in files)
            {
                parent.Children.Add(new FileTreeNode
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    IsDirectory = false
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignorer les dossiers sans acces
        }
    }
}
