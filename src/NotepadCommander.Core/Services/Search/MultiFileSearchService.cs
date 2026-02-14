using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Search;

public class MultiFileSearchService : IMultiFileSearchService
{
    private readonly ILogger<MultiFileSearchService> _logger;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", "node_modules", "bin", "obj", "__pycache__", ".vscode",
        ".svn", ".hg", "dist", "build", "packages", ".nuget", "TestResults"
    };

    public MultiFileSearchService(ILogger<MultiFileSearchService> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<FileSearchResult> SearchInDirectory(
        string directory,
        string pattern,
        MultiFileSearchOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(pattern) || !Directory.Exists(directory))
            yield break;

        var regex = BuildRegex(pattern, options.UseRegex, options.CaseSensitive, options.WholeWord);
        var resultCount = 0;
        var results = new ConcurrentQueue<FileSearchResult>();

        var files = EnumerateFiles(directory, options);

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        }, (filePath, ct) =>
        {
            if (resultCount >= options.MaxResults) return ValueTask.CompletedTask;
            if (IsBinaryFile(filePath)) return ValueTask.CompletedTask;

            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split('\n');

                for (var i = 0; i < lines.Length && resultCount < options.MaxResults; i++)
                {
                    var line = lines[i].TrimEnd('\r');
                    var matches = regex.Matches(line);

                    foreach (Match match in matches)
                    {
                        if (Interlocked.Increment(ref resultCount) > options.MaxResults)
                            break;

                        results.Enqueue(new FileSearchResult
                        {
                            FilePath = filePath,
                            Line = i + 1,
                            Column = match.Index + 1,
                            LineText = line.Length > 300 ? line[..300] + "..." : line,
                            MatchLength = match.Length
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error searching file {FilePath}", filePath);
            }

            return ValueTask.CompletedTask;
        });

        foreach (var result in results.OrderBy(r => r.FilePath).ThenBy(r => r.Line))
        {
            yield return result;
        }
    }

    private static IEnumerable<string> EnumerateFiles(string directory, MultiFileSearchOptions options)
    {
        var enumOptions = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true
        };

        var stack = new Stack<string>();
        stack.Push(directory);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();

            // Add subdirectories
            try
            {
                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var name = Path.GetFileName(subDir);
                    if (!IgnoredDirectories.Contains(name))
                        stack.Push(subDir);
                }
            }
            catch { /* skip inaccessible dirs */ }

            // Yield files
            string[] files;
            try
            {
                files = Directory.GetFiles(dir);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).TrimStart('.');

                if (options.IncludeExtensions is { Length: > 0 })
                {
                    if (!options.IncludeExtensions.Any(e =>
                        string.Equals(e.TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase)))
                        continue;
                }

                if (options.ExcludeExtensions is { Length: > 0 })
                {
                    if (options.ExcludeExtensions.Any(e =>
                        string.Equals(e.TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase)))
                        continue;
                }

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
        catch
        {
            return true;
        }
    }

    private static Regex BuildRegex(string pattern, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        var regexPattern = useRegex ? pattern : Regex.Escape(pattern);

        if (wholeWord)
            regexPattern = $@"\b{regexPattern}\b";

        var options = RegexOptions.Compiled;
        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        return new Regex(regexPattern, options);
    }
}
