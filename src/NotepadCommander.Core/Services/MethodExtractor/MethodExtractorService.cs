using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.MethodExtractor;

public class MethodExtractorService : IMethodExtractorService
{
    private readonly ILogger<MethodExtractorService> _logger;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", "node_modules", "bin", "obj", "__pycache__", ".vscode",
        ".svn", ".hg", "dist", "build", "packages", ".nuget", "TestResults"
    };

    private static readonly HashSet<string> BraceLanguageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".java", ".js", ".jsx", ".ts", ".tsx", ".go", ".rs", ".c", ".cpp", ".h", ".hpp",
        ".php", ".swift", ".kt", ".kts", ".scala", ".m", ".mm"
    };

    private static readonly HashSet<string> PythonExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".py", ".pyw"
    };

    public MethodExtractorService(ILogger<MethodExtractorService> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<MethodInfo> ExtractMethods(
        string directory,
        string[] methodNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory) || methodNames.Length == 0)
            yield break;

        var namesToFind = new HashSet<string>(methodNames, StringComparer.OrdinalIgnoreCase);
        var results = new ConcurrentBag<MethodInfo>();

        var files = EnumerateSourceFiles(directory);

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        }, (filePath, ct) =>
        {
            if (IsBinaryFile(filePath)) return ValueTask.CompletedTask;

            try
            {
                var ext = Path.GetExtension(filePath);
                var language = GetLanguageName(ext);

                string content;
                try { content = File.ReadAllText(filePath); }
                catch (UnauthorizedAccessException) { return ValueTask.CompletedTask; }
                catch (IOException) { return ValueTask.CompletedTask; }

                var lines = content.Split('\n');

                if (BraceLanguageExtensions.Contains(ext))
                {
                    ExtractBraceLanguageMethods(filePath, lines, namesToFind, language, results);
                }
                else if (PythonExtensions.Contains(ext))
                {
                    ExtractPythonMethods(filePath, lines, namesToFind, language, results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error extracting methods from {FilePath}", filePath);
            }

            return ValueTask.CompletedTask;
        });

        foreach (var result in results.OrderBy(r => r.FilePath).ThenBy(r => r.StartLine))
        {
            yield return result;
        }
    }

    private static void ExtractBraceLanguageMethods(
        string filePath, string[] lines, HashSet<string> methodNames, string language,
        ConcurrentBag<MethodInfo> results)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            foreach (var name in methodNames)
            {
                if (!IsMethodSignatureLine(line, name, language))
                    continue;

                var methodInfo = ExtractBraceMethod(filePath, lines, i, name, language);
                if (methodInfo != null)
                    results.Add(methodInfo);
            }
        }
    }

    private static bool IsMethodSignatureLine(string line, string methodName, string language)
    {
        var trimmed = line.TrimStart();

        // Skip comments
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            return false;

        // Check the line contains the method name followed by (
        var nameIndex = FindMethodNameInLine(trimmed, methodName);
        if (nameIndex < 0) return false;

        // For C#/Java/similar: typically has modifiers or return type before the name
        // For JS/TS: function keyword, or assignment, or class method
        var beforeName = trimmed[..nameIndex].TrimEnd();

        // JS/TS: function methodName(
        if (beforeName.EndsWith("function"))
            return true;

        // JS/TS: async function methodName(
        if (beforeName.EndsWith("async function"))
            return true;

        // JS/TS: methodName = ( or methodName = function(
        // Handle: const/let/var name = (...) => or const/let/var name = function(
        if (beforeName.EndsWith("=") || beforeName.EndsWith("= async"))
            return true;

        // C#/Java/Go/Rust/etc: return type + name, or access modifier + return type + name
        // If there's something before the name (return type, modifiers), it's likely a method definition
        if (beforeName.Length > 0 && !beforeName.EndsWith(".") && !beforeName.EndsWith("->") && !beforeName.EndsWith("::"))
        {
            // Verify it's not a method call (no = before, no . before)
            // Method definitions typically have type keywords or modifiers before
            return true;
        }

        // Class method shorthand (JS/TS): just methodName( at start of trimmed line (inside class)
        if (nameIndex == 0 || trimmed[..nameIndex].Trim() is "async" or "static" or "async static" or "static async"
            or "public" or "private" or "protected" or "override" or "virtual" or "abstract")
            return true;

        return false;
    }

    private static int FindMethodNameInLine(string line, string methodName)
    {
        var startIndex = 0;
        while (true)
        {
            var idx = line.IndexOf(methodName, startIndex, StringComparison.Ordinal);
            if (idx < 0) return -1;

            var afterIdx = idx + methodName.Length;

            // Check that name is followed by ( or whitespace then (
            if (afterIdx < line.Length)
            {
                var rest = line[afterIdx..].TrimStart();
                if (!rest.StartsWith("(") && !rest.StartsWith("<")) // generic methods like Method<T>(
                {
                    startIndex = afterIdx;
                    continue;
                }
            }
            else
            {
                startIndex = afterIdx;
                continue;
            }

            // Check that name is preceded by a word boundary
            if (idx > 0 && char.IsLetterOrDigit(line[idx - 1]))
            {
                startIndex = afterIdx;
                continue;
            }

            return idx;
        }
    }

    private static MethodInfo? ExtractBraceMethod(
        string filePath, string[] lines, int startLineIndex, string methodName, string language)
    {
        // Find the opening brace - it may be on the same line or a subsequent line (e.g., Allman style)
        var braceCount = 0;
        var foundOpenBrace = false;
        var bodyStartLine = startLineIndex;

        // Look for opening brace starting from the signature line
        for (var i = startLineIndex; i < lines.Length && i < startLineIndex + 10; i++)
        {
            var line = lines[i].TrimEnd('\r');

            // Count braces while ignoring those in strings and comments
            foreach (var ch in StripStringsAndComments(line))
            {
                if (ch == '{')
                {
                    if (!foundOpenBrace)
                    {
                        foundOpenBrace = true;
                        bodyStartLine = i;
                    }
                    braceCount++;
                }
                else if (ch == '}')
                {
                    braceCount--;
                }
            }

            if (foundOpenBrace && braceCount == 0)
            {
                return BuildMethodInfo(filePath, lines, startLineIndex, i, methodName, language);
            }

            if (foundOpenBrace)
                break;

            // For arrow functions: =>
            if (line.Contains("=>"))
            {
                // Single-line arrow function
                if (!line.Contains("{"))
                {
                    // Find end: next ; or end of line
                    return BuildMethodInfo(filePath, lines, startLineIndex, i, methodName, language);
                }
            }
        }

        if (!foundOpenBrace)
            return null;

        // Continue counting braces from the line after where we found the opening brace
        for (var i = bodyStartLine + 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            foreach (var ch in StripStringsAndComments(line))
            {
                if (ch == '{') braceCount++;
                else if (ch == '}') braceCount--;
            }

            if (braceCount == 0)
            {
                return BuildMethodInfo(filePath, lines, startLineIndex, i, methodName, language);
            }
        }

        return null;
    }

    private static void ExtractPythonMethods(
        string filePath, string[] lines, HashSet<string> methodNames, string language,
        ConcurrentBag<MethodInfo> results)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();

            foreach (var name in methodNames)
            {
                if (!trimmed.StartsWith($"def {name}(") && !trimmed.StartsWith($"async def {name}("))
                    continue;

                var defIndent = GetIndentation(line);
                var endLine = i;

                // Find the end of the method body (lines with greater indentation, or blank lines)
                for (var j = i + 1; j < lines.Length; j++)
                {
                    var bodyLine = lines[j].TrimEnd('\r');

                    // Blank lines are part of the body
                    if (string.IsNullOrWhiteSpace(bodyLine))
                    {
                        endLine = j;
                        continue;
                    }

                    var bodyIndent = GetIndentation(bodyLine);
                    if (bodyIndent <= defIndent)
                    {
                        // Trim trailing blank lines
                        while (endLine > i && string.IsNullOrWhiteSpace(lines[endLine].TrimEnd('\r')))
                            endLine--;
                        break;
                    }

                    endLine = j;
                }

                // Handle case where method is at end of file
                if (endLine == lines.Length - 1)
                {
                    while (endLine > i && string.IsNullOrWhiteSpace(lines[endLine].TrimEnd('\r')))
                        endLine--;
                }

                var methodInfo = BuildMethodInfo(filePath, lines, i, endLine, name, language);
                if (methodInfo != null)
                    results.Add(methodInfo);
            }
        }
    }

    private static int GetIndentation(string line)
    {
        var count = 0;
        foreach (var ch in line)
        {
            if (ch == ' ') count++;
            else if (ch == '\t') count += 4;
            else break;
        }
        return count;
    }

    private static MethodInfo BuildMethodInfo(
        string filePath, string[] lines, int startLine, int endLine, string methodName, string language)
    {
        var bodyLines = new List<string>();
        for (var i = startLine; i <= endLine && i < lines.Length; i++)
        {
            bodyLines.Add(lines[i].TrimEnd('\r'));
        }

        var signature = lines[startLine].TrimEnd('\r').Trim();

        return new MethodInfo
        {
            FilePath = filePath,
            MethodName = methodName,
            Signature = signature,
            FullBody = string.Join(Environment.NewLine, bodyLines),
            StartLine = startLine + 1, // 1-based
            EndLine = endLine + 1,
            Language = language
        };
    }

    private static string StripStringsAndComments(string line)
    {
        var result = new char[line.Length];
        var inString = false;
        var stringChar = '\0';
        var inLineComment = false;
        var len = 0;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (inLineComment)
            {
                result[len++] = ' ';
                continue;
            }

            if (inString)
            {
                if (ch == stringChar && (i == 0 || line[i - 1] != '\\'))
                    inString = false;
                result[len++] = ' ';
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                inString = true;
                stringChar = ch;
                result[len++] = ' ';
                continue;
            }

            if (ch == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                inLineComment = true;
                result[len++] = ' ';
                continue;
            }

            result[len++] = ch;
        }

        return new string(result, 0, len);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory)
    {
        var stack = new Stack<string>();
        stack.Push(directory);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();

            try
            {
                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var name = Path.GetFileName(subDir);
                    if (!IgnoredDirectories.Contains(name))
                        stack.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException) { /* skip inaccessible dirs */ }
            catch (IOException) { /* skip inaccessible dirs */ }

            string[] files;
            try { files = Directory.GetFiles(dir); }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (BraceLanguageExtensions.Contains(ext) || PythonExtensions.Contains(ext))
                    yield return file;
            }
        }
    }

    private static bool IsBinaryFile(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[512];
            var read = stream.Read(buffer, 0, buffer.Length);

            for (var i = 0; i < read; i++)
            {
                if (buffer[i] == 0) return true;
            }

            return false;
        }
        catch (UnauthorizedAccessException) { return true; }
        catch (IOException) { return true; }
    }

    private static string GetLanguageName(string extension) => extension.ToLowerInvariant() switch
    {
        ".cs" => "C#",
        ".java" => "Java",
        ".js" or ".jsx" => "JavaScript",
        ".ts" or ".tsx" => "TypeScript",
        ".py" or ".pyw" => "Python",
        ".go" => "Go",
        ".rs" => "Rust",
        ".c" or ".h" => "C",
        ".cpp" or ".hpp" or ".cc" => "C++",
        ".php" => "PHP",
        ".swift" => "Swift",
        ".kt" or ".kts" => "Kotlin",
        ".scala" => "Scala",
        ".m" or ".mm" => "Objective-C",
        _ => "Unknown"
    };
}
