namespace NotepadCommander.Core.Services.Session;

public interface ISessionService
{
    SessionData? LoadSession();
    void SaveSession(SessionData session);
    void ClearSession();
}

public class SessionData
{
    public List<SessionTab> Tabs { get; set; } = new();
    public int ActiveTabIndex { get; set; }
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;
}

public class SessionTab
{
    public string? FilePath { get; set; }
    public string? UnsavedContent { get; set; }
    public int CursorLine { get; set; } = 1;
    public int CursorColumn { get; set; } = 1;
}
