namespace NotepadCommander.UI.Models;

public class SearchMatchItem
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string LineText { get; set; } = string.Empty;
    public int MatchLength { get; set; }
}
