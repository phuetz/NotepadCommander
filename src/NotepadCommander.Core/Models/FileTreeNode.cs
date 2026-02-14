namespace NotepadCommander.Core.Models;

public class FileTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public bool IsExpanded { get; set; }
    public List<FileTreeNode> Children { get; set; } = new();

    public string Icon => IsDirectory ? (IsExpanded ? "📂" : "📁") : GetFileIcon();

    private string GetFileIcon()
    {
        var ext = Path.GetExtension(Name).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "🟢",
            ".js" or ".ts" => "🟡",
            ".py" => "🔵",
            ".json" or ".xml" or ".yaml" or ".yml" => "📋",
            ".md" => "📝",
            ".html" or ".htm" => "🌐",
            ".css" => "🎨",
            _ => "📄"
        };
    }
}
