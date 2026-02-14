using System.Text.RegularExpressions;

namespace NotepadCommander.Core.Services;

public class SearchReplaceService : ISearchReplaceService
{
    public IReadOnlyList<SearchResult> FindAll(string text, string pattern, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return Array.Empty<SearchResult>();

        var regex = BuildRegex(pattern, useRegex, caseSensitive, wholeWord);
        var matches = regex.Matches(text);
        var results = new List<SearchResult>();

        foreach (Match match in matches)
        {
            var (line, col) = GetLineAndColumn(text, match.Index);
            results.Add(new SearchResult
            {
                Index = match.Index,
                Length = match.Length,
                Value = match.Value,
                Line = line,
                Column = col
            });
        }

        return results;
    }

    public SearchResult? FindNext(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return null;

        var regex = BuildRegex(pattern, useRegex, caseSensitive, wholeWord);
        var match = regex.Match(text, Math.Min(startIndex, text.Length));

        if (!match.Success)
        {
            // Reprendre depuis le debut
            match = regex.Match(text, 0);
        }

        if (!match.Success) return null;

        var (line, col) = GetLineAndColumn(text, match.Index);
        return new SearchResult
        {
            Index = match.Index,
            Length = match.Length,
            Value = match.Value,
            Line = line,
            Column = col
        };
    }

    public SearchResult? FindPrevious(string text, string pattern, int startIndex, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return null;

        var regex = BuildRegex(pattern, useRegex, caseSensitive, wholeWord);
        var matches = regex.Matches(text);

        Match? lastBefore = null;
        foreach (Match match in matches)
        {
            if (match.Index < startIndex)
                lastBefore = match;
            else
                break;
        }

        if (lastBefore == null && matches.Count > 0)
        {
            // Reprendre depuis la fin
            lastBefore = matches[^1];
        }

        if (lastBefore == null) return null;

        var (line, col) = GetLineAndColumn(text, lastBefore.Index);
        return new SearchResult
        {
            Index = lastBefore.Index,
            Length = lastBefore.Length,
            Value = lastBefore.Value,
            Line = line,
            Column = col
        };
    }

    public string ReplaceAll(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return text;

        var regex = BuildRegex(pattern, useRegex, caseSensitive, wholeWord);
        return regex.Replace(text, replacement);
    }

    public (string result, int count) ReplaceAllWithCount(string text, string pattern, string replacement, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return (text, 0);

        var regex = BuildRegex(pattern, useRegex, caseSensitive, wholeWord);
        var count = regex.Matches(text).Count;
        var result = regex.Replace(text, replacement);
        return (result, count);
    }

    private static Regex BuildRegex(string pattern, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        var regexPattern = useRegex ? pattern : Regex.Escape(pattern);

        if (wholeWord)
            regexPattern = $@"\b{regexPattern}\b";

        var options = RegexOptions.Multiline;
        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        return new Regex(regexPattern, options);
    }

    private static (int line, int column) GetLineAndColumn(string text, int index)
    {
        var line = 1;
        var lastNewLine = 0;

        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lastNewLine = i + 1;
            }
        }

        return (line, index - lastNewLine + 1);
    }
}
