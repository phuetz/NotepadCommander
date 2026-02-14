namespace NotepadCommander.Core.Services.Git;

public enum GitChangeType
{
    Added,
    Modified,
    Deleted
}

public class GitLineChange
{
    public int LineNumber { get; set; }
    public GitChangeType ChangeType { get; set; }
}

public interface IGitService
{
    bool IsInGitRepository(string filePath);
    IReadOnlyList<GitLineChange> GetModifiedLines(string filePath);
}
