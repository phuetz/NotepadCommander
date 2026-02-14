namespace NotepadCommander.Core.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Load();
    void Save();
    void Reset();
}

public class AppSettings
{
    public double FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Cascadia Code";
    public bool ShowLineNumbers { get; set; } = true;
    public bool WordWrap { get; set; } = false;
    public bool ShowMinimap { get; set; } = false;
    public int TabSize { get; set; } = 4;
    public bool UseSpaces { get; set; } = true;
    public bool AutoIndent { get; set; } = true;
    public string Theme { get; set; } = "Light";
    public bool AutoSave { get; set; } = false;
    public int AutoSaveIntervalSeconds { get; set; } = 60;
    public double ZoomLevel { get; set; } = 100;
    public bool HighlightCurrentLine { get; set; } = true;
    public bool ShowWhitespace { get; set; } = false;
    public bool BracketMatching { get; set; } = true;
}
