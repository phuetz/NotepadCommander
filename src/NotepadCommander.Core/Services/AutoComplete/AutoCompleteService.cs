using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.AutoComplete;

public class AutoCompleteService : IAutoCompleteService
{
    private static readonly Dictionary<SupportedLanguage, string[]> LanguageKeywords = new()
    {
        [SupportedLanguage.CSharp] = new[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var",
            "virtual", "void", "volatile", "while", "async", "await", "record", "required"
        },
        [SupportedLanguage.JavaScript] = new[]
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
            "do", "else", "export", "extends", "false", "finally", "for", "function", "if", "import",
            "in", "instanceof", "let", "new", "null", "return", "super", "switch", "this", "throw",
            "true", "try", "typeof", "undefined", "var", "void", "while", "with", "yield",
            "async", "await", "of", "static", "get", "set"
        },
        [SupportedLanguage.Python] = new[]
        {
            "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
            "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
            "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise",
            "return", "try", "while", "with", "yield", "self", "print", "range", "len", "list",
            "dict", "set", "tuple", "int", "str", "float", "bool", "type", "input"
        },
        [SupportedLanguage.TypeScript] = new[]
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
            "do", "else", "enum", "export", "extends", "false", "finally", "for", "function", "if",
            "implements", "import", "in", "instanceof", "interface", "let", "new", "null", "package",
            "private", "protected", "public", "return", "static", "super", "switch", "this", "throw",
            "true", "try", "type", "typeof", "undefined", "var", "void", "while", "with", "yield",
            "async", "await", "of", "readonly", "abstract", "as", "any", "boolean", "number", "string",
            "never", "unknown", "keyof", "infer", "declare", "module", "namespace"
        }
    };

    public List<string> GetSuggestions(string text, int cursorPosition, SupportedLanguage language, int maxResults = 20)
    {
        if (string.IsNullOrEmpty(text) || cursorPosition <= 0)
            return new List<string>();

        var prefix = GetCurrentWord(text, cursorPosition);
        if (string.IsNullOrEmpty(prefix) || prefix.Length < 2)
            return new List<string>();

        var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add language keywords
        if (LanguageKeywords.TryGetValue(language, out var keywords))
        {
            foreach (var kw in keywords.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                suggestions.Add(kw);
        }

        // Add words from current document
        var words = ExtractWords(text);
        foreach (var word in words.Where(w =>
            w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(w, prefix, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(word);
        }

        return suggestions
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    internal static string GetCurrentWord(string text, int cursorPosition)
    {
        var pos = Math.Min(cursorPosition, text.Length) - 1;
        var start = pos;

        while (start >= 0 && (char.IsLetterOrDigit(text[start]) || text[start] == '_'))
            start--;

        start++;
        return text.Substring(start, pos - start + 1);
    }

    internal static HashSet<string> ExtractWords(string text)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var word = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                word.Append(ch);
            }
            else
            {
                if (word.Length >= 3)
                    words.Add(word.ToString());
                word.Clear();
            }
        }

        if (word.Length >= 3)
            words.Add(word.ToString());

        return words;
    }
}
