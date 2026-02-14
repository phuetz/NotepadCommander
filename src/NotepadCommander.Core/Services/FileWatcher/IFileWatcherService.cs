namespace NotepadCommander.Core.Services.FileWatcher;

public interface IFileWatcherService
{
    void WatchFile(string filePath);
    void UnwatchFile(string filePath);
    void UnwatchAll();
    event Action<string> FileChanged;
    event Action<string> FileDeleted;
}
