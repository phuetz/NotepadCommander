namespace NotepadCommander.Core.Services;

public interface IRecentFilesService
{
    IReadOnlyList<string> RecentFiles { get; }
    IReadOnlyList<string> PinnedFiles { get; }
    void AddFile(string filePath);
    void RemoveFile(string filePath);
    void PinFile(string filePath);
    void UnpinFile(string filePath);
    void Clear();
    void Load();
    void Save();
}
