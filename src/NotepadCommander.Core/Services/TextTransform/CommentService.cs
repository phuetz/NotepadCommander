using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.TextTransform;

public class CommentService : ICommentService
{
    private static readonly Dictionary<SupportedLanguage, string> CommentPrefixes = new()
    {
        [SupportedLanguage.CSharp] = "//",
        [SupportedLanguage.JavaScript] = "//",
        [SupportedLanguage.TypeScript] = "//",
        [SupportedLanguage.Java] = "//",
        [SupportedLanguage.Cpp] = "//",
        [SupportedLanguage.C] = "//",
        [SupportedLanguage.Go] = "//",
        [SupportedLanguage.Rust] = "//",
        [SupportedLanguage.Php] = "//",
        [SupportedLanguage.Python] = "#",
        [SupportedLanguage.Ruby] = "#",
        [SupportedLanguage.Bash] = "#",
        [SupportedLanguage.PowerShell] = "#",
        [SupportedLanguage.Yaml] = "#",
        [SupportedLanguage.Sql] = "--",
        [SupportedLanguage.Html] = "<!--",
        [SupportedLanguage.Xml] = "<!--",
        [SupportedLanguage.Css] = "/*",
    };

    private static readonly Dictionary<SupportedLanguage, string> CommentSuffixes = new()
    {
        [SupportedLanguage.Html] = "-->",
        [SupportedLanguage.Xml] = "-->",
        [SupportedLanguage.Css] = "*/",
    };

    public string? GetCommentPrefix(SupportedLanguage language)
    {
        return CommentPrefixes.TryGetValue(language, out var prefix) ? prefix : null;
    }

    public string ToggleComment(string text, SupportedLanguage language)
    {
        var prefix = GetCommentPrefix(language);
        if (prefix == null) return text;

        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var allCommented = lines.Where(l => !string.IsNullOrWhiteSpace(l))
            .All(l => l.TrimStart().StartsWith(prefix));

        return allCommented
            ? UncommentLinesInternal(lines, language)
            : CommentLinesInternal(lines, language);
    }

    public string CommentLines(string text, SupportedLanguage language)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        return CommentLinesInternal(lines, language);
    }

    public string UncommentLines(string text, SupportedLanguage language)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        return UncommentLinesInternal(lines, language);
    }

    private string CommentLinesInternal(string[] lines, SupportedLanguage language)
    {
        var prefix = GetCommentPrefix(language);
        if (prefix == null) return string.Join(Environment.NewLine, lines);

        var hasSuffix = CommentSuffixes.TryGetValue(language, out var suffix);

        var commented = lines.Select(line =>
        {
            if (string.IsNullOrWhiteSpace(line)) return line;
            return hasSuffix
                ? $"{prefix} {line} {suffix}"
                : $"{prefix} {line}";
        });

        return string.Join(Environment.NewLine, commented);
    }

    private string UncommentLinesInternal(string[] lines, SupportedLanguage language)
    {
        var prefix = GetCommentPrefix(language);
        if (prefix == null) return string.Join(Environment.NewLine, lines);

        var hasSuffix = CommentSuffixes.TryGetValue(language, out var suffix);

        var uncommented = lines.Select(line =>
        {
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith(prefix)) return line;

            var leadingSpaces = line[..^trimmed.Length];
            var content = trimmed[prefix.Length..];
            if (content.StartsWith(" ")) content = content[1..];

            if (hasSuffix && suffix != null && content.TrimEnd().EndsWith(suffix))
            {
                var idx = content.LastIndexOf(suffix, StringComparison.Ordinal);
                content = content[..idx];
                if (content.EndsWith(" ")) content = content[..^1];
            }

            return leadingSpaces + content;
        });

        return string.Join(Environment.NewLine, uncommented);
    }
}
