using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.FileWatcher;

public class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string>? FileChanged;
    public event Action<string>? FileDeleted;

    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
    }

    public void WatchFile(string filePath)
    {
        if (_watchers.ContainsKey(filePath)) return;

        var dir = Path.GetDirectoryName(filePath);
        var name = Path.GetFileName(filePath);

        if (dir == null || !Directory.Exists(dir)) return;

        var watcher = new FileSystemWatcher(dir, name)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, e) => FileChanged?.Invoke(e.FullPath);
        watcher.Deleted += (_, e) => FileDeleted?.Invoke(e.FullPath);
        watcher.Renamed += (_, e) => FileDeleted?.Invoke(e.OldFullPath);

        _watchers[filePath] = watcher;
        _logger.LogDebug("Surveillance du fichier : {FilePath}", filePath);
    }

    public void UnwatchFile(string filePath)
    {
        if (_watchers.TryGetValue(filePath, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _watchers.Remove(filePath);
        }
    }

    public void UnwatchAll()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    public void Dispose()
    {
        UnwatchAll();
        GC.SuppressFinalize(this);
    }
}
