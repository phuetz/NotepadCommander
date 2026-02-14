using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;

    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotepadCommander");

    private static readonly string SettingsPath = Path.Combine(DataDir, "settings.json");

    public AppSettings Settings { get; private set; } = new();

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings != null)
                Settings = settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de charger les parametres");
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de sauvegarder les parametres");
        }
    }

    public void Reset()
    {
        Settings = new AppSettings();
        Save();
    }
}
