namespace NotepadCommander.Core.Services.Compare;

public interface ICompareService
{
    DiffResult Compare(string oldText, string newText);
}

public class DiffResult
{
    public List<DiffLine> Lines { get; set; } = new();
}

public class DiffLine
{
    public DiffLineType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
}

public enum DiffLineType
{
    Unchanged,
    Inserted,
    Deleted,
    Modified
}
