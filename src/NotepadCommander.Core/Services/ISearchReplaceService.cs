namespace NotepadCommander.Core.Services;

public interface ISearchReplaceService
{
    IReadOnlyList<SearchResult> FindAll(string text, string pattern, bool useRegex, bool caseSensitive, bool wholeWord);
    SearchResult? FindNext(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord);
    SearchResult? FindPrevious(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord);
    string ReplaceAll(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord);
    (string result, int count) ReplaceAllWithCount(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord);
}

public class SearchResult
{
    public int Index { get; set; }
    public int Length { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
