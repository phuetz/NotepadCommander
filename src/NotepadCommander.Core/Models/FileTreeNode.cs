namespace NotepadCommander.Core.Models;

public class FileTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public bool IsExpanded { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public List<FileTreeNode> Children { get; set; } = new();

    public string Extension => IsDirectory ? string.Empty : Path.GetExtension(Name).ToLowerInvariant();

    public string FormattedSize
    {
        get
        {
            if (IsDirectory) return string.Empty;
            return FileSize switch
            {
                < 1024 => $"{FileSize} o",
                < 1024 * 1024 => $"{FileSize / 1024.0:F1} Ko",
                < 1024 * 1024 * 1024 => $"{FileSize / (1024.0 * 1024):F1} Mo",
                _ => $"{FileSize / (1024.0 * 1024 * 1024):F1} Go"
            };
        }
    }

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
