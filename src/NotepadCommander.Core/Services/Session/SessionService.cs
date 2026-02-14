using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.Session;

public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;

    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotepadCommander");

    private static readonly string SessionPath = Path.Combine(DataDir, "session.json");

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    public SessionData? LoadSession()
    {
        try
        {
            if (!File.Exists(SessionPath)) return null;

            var json = File.ReadAllText(SessionPath);
            return JsonSerializer.Deserialize<SessionData>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de charger la session");
            return null;
        }
    }

    public void SaveSession(SessionData session)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SessionPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de sauvegarder la session");
        }
    }

    public void ClearSession()
    {
        try
        {
            if (File.Exists(SessionPath))
                File.Delete(SessionPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de supprimer la session");
        }
    }
}
