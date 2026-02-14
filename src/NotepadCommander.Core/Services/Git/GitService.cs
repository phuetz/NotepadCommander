using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.Git;

public class GitService : IGitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    public bool IsInGitRepository(string filePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir)) return false;

            var output = RunGit("rev-parse --git-dir", dir);
            return output != null;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<GitLineChange> GetModifiedLines(string filePath)
    {
        var changes = new List<GitLineChange>();

        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir)) return changes;

            // Try diff against HEAD first (tracked file)
            var output = RunGit($"diff HEAD -- \"{filePath}\"", dir);

            if (output == null)
                return changes;

            // If empty output, try diff without HEAD (new untracked file)
            if (string.IsNullOrWhiteSpace(output))
            {
                // Check if file is tracked
                var tracked = RunGit($"ls-files -- \"{filePath}\"", dir);
                if (string.IsNullOrWhiteSpace(tracked))
                {
                    // Untracked file: all lines are "added"
                    var lineCount = File.Exists(filePath) ? File.ReadAllLines(filePath).Length : 0;
                    for (var i = 1; i <= lineCount; i++)
                        changes.Add(new GitLineChange { LineNumber = i, ChangeType = GitChangeType.Added });
                    return changes;
                }
                return changes; // No changes
            }

            ParseUnifiedDiff(output, changes);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get git diff for {FilePath}", filePath);
        }

        return changes;
    }

    private static void ParseUnifiedDiff(string diff, List<GitLineChange> changes)
    {
        var hunkRegex = new Regex(@"^@@\s+\-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@", RegexOptions.Multiline);
        var lines = diff.Split('\n');

        var i = 0;
        while (i < lines.Length)
        {
            var match = hunkRegex.Match(lines[i]);
            if (!match.Success)
            {
                i++;
                continue;
            }

            var oldStart = int.Parse(match.Groups[1].Value);
            var oldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
            var newStart = int.Parse(match.Groups[3].Value);
            var newCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1;

            i++;
            var newLine = newStart;
            var deletedAtLine = newStart;

            while (i < lines.Length && !lines[i].StartsWith("@@") && !lines[i].StartsWith("diff "))
            {
                if (lines[i].StartsWith("+"))
                {
                    // Check if this is a modification (corresponding - line before)
                    var changeType = GitChangeType.Added;
                    // Look back for a deletion at same position
                    if (changes.Any(c => c.LineNumber == newLine && c.ChangeType == GitChangeType.Deleted))
                    {
                        // Convert the deleted to modified
                        var existing = changes.First(c => c.LineNumber == newLine && c.ChangeType == GitChangeType.Deleted);
                        existing.ChangeType = GitChangeType.Modified;
                        changeType = GitChangeType.Modified;
                    }

                    if (changeType == GitChangeType.Added)
                        changes.Add(new GitLineChange { LineNumber = newLine, ChangeType = GitChangeType.Added });

                    newLine++;
                }
                else if (lines[i].StartsWith("-"))
                {
                    // Mark deletion at the current new-file position
                    if (!changes.Any(c => c.LineNumber == deletedAtLine && c.ChangeType == GitChangeType.Deleted))
                    {
                        changes.Add(new GitLineChange { LineNumber = deletedAtLine, ChangeType = GitChangeType.Deleted });
                    }
                }
                else if (lines[i].StartsWith(" ") || lines[i] == "")
                {
                    newLine++;
                    deletedAtLine = newLine;
                }
                else if (lines[i].StartsWith("\\"))
                {
                    // "\ No newline at end of file" - skip
                }

                i++;
            }
        }
    }

    private string? RunGit(string arguments, string workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to run git {Arguments}", arguments);
            return null;
        }
    }
}
