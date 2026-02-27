using System.Collections.ObjectModel;

namespace NotepadCommander.UI.Models;

public class SearchFileGroup
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public int MatchCount { get; set; }
    public ObservableCollection<SearchMatchItem> Matches { get; set; } = new();
}
