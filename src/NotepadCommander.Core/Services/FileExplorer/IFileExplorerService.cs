using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.FileExplorer;

public interface IFileExplorerService
{
    FileTreeNode? LoadDirectory(string path);
    void ExpandNode(FileTreeNode node);
    FileTreeNode? FilterTree(FileTreeNode root, string filter);
    FileTreeNode? FilterTreeByExtensions(FileTreeNode root, IReadOnlySet<string> extensions);
    void RefreshNode(FileTreeNode node);
    bool CreateFile(string parentPath, string fileName);
    bool CreateDirectory(string parentPath, string dirName);
    bool Delete(string path);
    bool Rename(string oldPath, string newName);
    (int files, int folders) CountNodes(FileTreeNode node);
}
