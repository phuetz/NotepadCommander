namespace NotepadCommander.Core.Models;

public class FileSearchResult
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string LineText { get; set; } = string.Empty;
    public int MatchLength { get; set; }
}

public class MultiFileSearchOptions
{
    public bool UseRegex { get; set; }
    public bool CaseSensitive { get; set; }
    public bool WholeWord { get; set; }
    public string[]? IncludeExtensions { get; set; }
    public string[]? ExcludeExtensions { get; set; }
    public int MaxResults { get; set; } = 500;
}
