using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.FileExplorer;

public interface IFileExplorerService
{
    FileTreeNode? LoadDirectory(string path);
    void ExpandNode(FileTreeNode node);
}
