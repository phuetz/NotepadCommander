using CommunityToolkit.Mvvm.ComponentModel;

namespace NotepadCommander.UI.Services;

/// <summary>
/// Manages editor (TextMate) themes independently from Chrome UI theme.
/// Two-layer approach inspired by AvalonStudio: Chrome theme (Light/Dark via ThemeDictionaries)
/// can differ from editor syntax theme (DarkPlus, LightPlus, etc.).
/// </summary>
public partial class EditorThemeService : ObservableObject
{
    public static readonly string[] AvailableThemes =
    {
        "Light Plus",
        "Dark Plus",
        "Monokai",
        "Solarized Light",
        "Solarized Dark"
    };

    [ObservableProperty]
    private string currentEditorTheme = "Light Plus";

    [ObservableProperty]
    private bool followChromeTheme = true;

    /// <summary>
    /// Returns whether the current editor theme is dark.
    /// Used by NotepadEditor to select TextMate theme.
    /// </summary>
    public bool IsDarkEditorTheme => CurrentEditorTheme.Contains("Dark") || CurrentEditorTheme == "Monokai";

    /// <summary>
    /// Raised when the editor theme changes. NotepadEditor listens to this.
    /// </summary>
    public event Action<bool>? EditorThemeChanged;

    /// <summary>
    /// Apply theme based on Chrome UI theme (when FollowChromeTheme is enabled).
    /// </summary>
    public void SyncWithChromeTheme(string chromeTheme)
    {
        if (!FollowChromeTheme) return;

        var newTheme = chromeTheme == "Dark" ? "Dark Plus" : "Light Plus";
        if (newTheme != CurrentEditorTheme)
        {
            CurrentEditorTheme = newTheme;
        }
    }

    /// <summary>
    /// Explicitly set the editor theme (disables FollowChromeTheme).
    /// </summary>
    public void SetEditorTheme(string themeName)
    {
        FollowChromeTheme = false;
        CurrentEditorTheme = themeName;
    }

    partial void OnCurrentEditorThemeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDarkEditorTheme));
        EditorThemeChanged?.Invoke(IsDarkEditorTheme);
    }
}
