using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services;

public class RecentFilesService : IRecentFilesService
{
    private readonly ILogger<RecentFilesService> _logger;
    private readonly List<string> _recentFiles = new();
    private readonly List<string> _pinnedFiles = new();
    private const int MaxRecentFiles = 20;

    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotepadCommander");

    private static readonly string RecentFilesPath = Path.Combine(DataDir, "recent-files.json");

    public RecentFilesService(ILogger<RecentFilesService> logger)
    {
        _logger = logger;
        Load();
    }

    public IReadOnlyList<string> RecentFiles => _recentFiles.AsReadOnly();
    public IReadOnlyList<string> PinnedFiles => _pinnedFiles.AsReadOnly();

    public void AddFile(string filePath)
    {
        var normalized = Path.GetFullPath(filePath);

        _recentFiles.RemoveAll(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
        _recentFiles.Insert(0, normalized);

        while (_recentFiles.Count > MaxRecentFiles)
        {
            var last = _recentFiles[^1];
            if (!_pinnedFiles.Any(p => string.Equals(p, last, StringComparison.OrdinalIgnoreCase)))
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }
            else
            {
                break;
            }
        }

        Save();
    }

    public void RemoveFile(string filePath)
    {
        var normalized = Path.GetFullPath(filePath);
        _recentFiles.RemoveAll(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
        _pinnedFiles.RemoveAll(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public void PinFile(string filePath)
    {
        var normalized = Path.GetFullPath(filePath);
        if (_pinnedFiles.Any(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase)))
            return;

        _pinnedFiles.Insert(0, normalized);
        Save();
    }

    public void UnpinFile(string filePath)
    {
        var normalized = Path.GetFullPath(filePath);
        _pinnedFiles.RemoveAll(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public void Clear()
    {
        _recentFiles.Clear();
        _pinnedFiles.Clear();
        Save();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(RecentFilesPath))
                return;

            var json = File.ReadAllText(RecentFilesPath);
            var data = JsonSerializer.Deserialize<RecentFilesData>(json);
            if (data == null) return;

            _recentFiles.Clear();
            _recentFiles.AddRange(data.RecentFiles ?? []);
            _pinnedFiles.Clear();
            _pinnedFiles.AddRange(data.PinnedFiles ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de charger les fichiers recents");
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var data = new RecentFilesData
            {
                RecentFiles = _recentFiles,
                PinnedFiles = _pinnedFiles
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RecentFilesPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de sauvegarder les fichiers recents");
        }
    }

    private class RecentFilesData
    {
        public List<string> RecentFiles { get; set; } = new();
        public List<string> PinnedFiles { get; set; } = new();
    }
}
