using AvaloniaEdit.Folding;
using NotepadCommander.Core.Models;
using TextDocument = AvaloniaEdit.Document.TextDocument;

namespace NotepadCommander.UI.Services;

public static class CodeFoldingService
{
    public static void UpdateFoldings(FoldingManager manager, TextDocument document, SupportedLanguage language)
    {
        IEnumerable<NewFolding> foldings;

        if (IsBraceLanguage(language))
            foldings = CreateBraceFoldings(document);
        else if (IsIndentLanguage(language))
            foldings = CreateIndentFoldings(document);
        else if (IsXmlLanguage(language))
            foldings = CreateXmlFoldings(document);
        else
            foldings = Enumerable.Empty<NewFolding>();

        var sorted = foldings.OrderBy(f => f.StartOffset).ToList();
        manager.UpdateFoldings(sorted, -1);
    }

    private static bool IsBraceLanguage(SupportedLanguage lang) => lang is
        SupportedLanguage.CSharp or SupportedLanguage.JavaScript or SupportedLanguage.TypeScript or
        SupportedLanguage.Java or SupportedLanguage.Go or SupportedLanguage.Rust or
        SupportedLanguage.Cpp or SupportedLanguage.C or SupportedLanguage.Css or
        SupportedLanguage.Json or SupportedLanguage.Php;

    private static bool IsIndentLanguage(SupportedLanguage lang) => lang is
        SupportedLanguage.Python or SupportedLanguage.Yaml;

    private static bool IsXmlLanguage(SupportedLanguage lang) => lang is
        SupportedLanguage.Html or SupportedLanguage.Xml;

    private static IEnumerable<NewFolding> CreateBraceFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var stack = new Stack<int>();
        var text = document.Text;

        var inString = false;
        var inLineComment = false;
        var inBlockComment = false;
        char stringChar = '\0';

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (inLineComment)
            {
                if (ch == '\n') inLineComment = false;
                continue;
            }

            if (inBlockComment)
            {
                if (ch == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                }
                continue;
            }

            if (inString)
            {
                if (ch == '\\') { i++; continue; }
                if (ch == stringChar) inString = false;
                continue;
            }

            if (ch == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (ch == '/' && next == '*') { inBlockComment = true; i++; continue; }
            if (ch == '"' || ch == '\'') { inString = true; stringChar = ch; continue; }

            if (ch == '{')
            {
                stack.Push(i);
            }
            else if (ch == '}' && stack.Count > 0)
            {
                var openOffset = stack.Pop();
                // Only create folding if it spans multiple lines
                var openLine = document.GetLineByOffset(openOffset);
                var closeLine = document.GetLineByOffset(i);
                if (closeLine.LineNumber > openLine.LineNumber + 1)
                {
                    foldings.Add(new NewFolding(openOffset, i + 1) { Name = "{...}" });
                }
            }
        }

        return foldings;
    }

    private static IEnumerable<NewFolding> CreateIndentFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var lineCount = document.LineCount;

        for (var i = 1; i <= lineCount; i++)
        {
            var line = document.GetLineByNumber(i);
            var text = document.GetText(line.Offset, line.Length);

            if (string.IsNullOrWhiteSpace(text)) continue;

            var indent = GetIndentLevel(text);

            // Look ahead for the end of this block
            var blockEnd = i;
            for (var j = i + 1; j <= lineCount; j++)
            {
                var nextLine = document.GetLineByNumber(j);
                var nextText = document.GetText(nextLine.Offset, nextLine.Length);

                if (string.IsNullOrWhiteSpace(nextText)) continue;

                var nextIndent = GetIndentLevel(nextText);
                if (nextIndent <= indent)
                {
                    blockEnd = j - 1;
                    break;
                }

                blockEnd = j;
            }

            if (blockEnd > i + 1)
            {
                var endLine = document.GetLineByNumber(blockEnd);
                foldings.Add(new NewFolding(line.Offset, endLine.EndOffset) { Name = "..." });
                // Skip lines we've already processed
                i = blockEnd;
            }
        }

        return foldings;
    }

    private static IEnumerable<NewFolding> CreateXmlFoldings(TextDocument document)
    {
        // Simple XML folding: match <tag> ... </tag> across multiple lines
        var foldings = new List<NewFolding>();
        var text = document.Text;
        var stack = new Stack<(string tag, int offset)>();

        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                if (i + 1 < text.Length && text[i + 1] == '/')
                {
                    // Closing tag
                    var end = text.IndexOf('>', i);
                    if (end > 0)
                    {
                        var tagName = text[(i + 2)..end].Trim();
                        // Find matching open tag
                        if (stack.Count > 0)
                        {
                            var (openTag, openOffset) = stack.Peek();
                            if (string.Equals(openTag, tagName, StringComparison.OrdinalIgnoreCase))
                            {
                                stack.Pop();
                                var openLine = document.GetLineByOffset(openOffset);
                                var closeLine = document.GetLineByOffset(end);
                                if (closeLine.LineNumber > openLine.LineNumber + 1)
                                {
                                    foldings.Add(new NewFolding(openOffset, end + 1) { Name = $"<{tagName}>...</{tagName}>" });
                                }
                            }
                        }
                        i = end + 1;
                        continue;
                    }
                }
                else if (i + 1 < text.Length && text[i + 1] != '!' && text[i + 1] != '?')
                {
                    // Opening tag
                    var end = text.IndexOf('>', i);
                    if (end > 0)
                    {
                        // Check if self-closing
                        if (text[end - 1] != '/')
                        {
                            var tagContent = text[(i + 1)..end];
                            var spaceIdx = tagContent.IndexOfAny(new[] { ' ', '\t', '\n', '\r' });
                            var tagName = spaceIdx > 0 ? tagContent[..spaceIdx] : tagContent;
                            stack.Push((tagName, i));
                        }
                        i = end + 1;
                        continue;
                    }
                }
            }
            i++;
        }

        return foldings;
    }

    private static int GetIndentLevel(string line)
    {
        var spaces = 0;
        foreach (var ch in line)
        {
            if (ch == ' ') spaces++;
            else if (ch == '\t') spaces += 4;
            else break;
        }
        return spaces;
    }
}
