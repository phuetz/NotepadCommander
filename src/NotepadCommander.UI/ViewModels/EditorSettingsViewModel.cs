using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Services;

namespace NotepadCommander.UI.ViewModels;

public partial class EditorSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool showLineNumbers;

    [ObservableProperty]
    private bool wordWrap;

    [ObservableProperty]
    private string currentTheme;

    [ObservableProperty]
    private bool isFullScreen;

    [ObservableProperty]
    private bool highlightCurrentLine;

    [ObservableProperty]
    private bool showWhitespace;

    [ObservableProperty]
    private bool showMinimap;

    [ObservableProperty]
    private double zoomLevel;

    [ObservableProperty]
    private bool isSidePanelVisible;

    public EditorSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        ShowLineNumbers = settingsService.Settings.ShowLineNumbers;
        WordWrap = settingsService.Settings.WordWrap;
        CurrentTheme = settingsService.Settings.Theme;
        HighlightCurrentLine = settingsService.Settings.HighlightCurrentLine;
        ShowWhitespace = settingsService.Settings.ShowWhitespace;
        ShowMinimap = settingsService.Settings.ShowMinimap;
        ZoomLevel = settingsService.Settings.ZoomLevel;
    }

    [RelayCommand]
    public void ToggleWordWrap()
    {
        WordWrap = !WordWrap;
        _settingsService.Settings.WordWrap = WordWrap;
    }

    [RelayCommand]
    public void ToggleLineNumbers()
    {
        ShowLineNumbers = !ShowLineNumbers;
        _settingsService.Settings.ShowLineNumbers = ShowLineNumbers;
    }

    [RelayCommand]
    public void SetThemeLight()
    {
        CurrentTheme = "Light";
        _settingsService.Settings.Theme = "Light";
    }

    [RelayCommand]
    public void SetThemeDark()
    {
        CurrentTheme = "Dark";
        _settingsService.Settings.Theme = "Dark";
    }

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 10, 300);
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 10, 50);
    }

    [RelayCommand]
    public void ZoomReset()
    {
        ZoomLevel = 100;
    }

    [RelayCommand]
    public void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
    }

    [RelayCommand]
    public void ToggleHighlightCurrentLine()
    {
        HighlightCurrentLine = !HighlightCurrentLine;
        _settingsService.Settings.HighlightCurrentLine = HighlightCurrentLine;
    }

    [RelayCommand]
    public void ToggleShowWhitespace()
    {
        ShowWhitespace = !ShowWhitespace;
        _settingsService.Settings.ShowWhitespace = ShowWhitespace;
    }

    [RelayCommand]
    public void ToggleMinimap()
    {
        ShowMinimap = !ShowMinimap;
        _settingsService.Settings.ShowMinimap = ShowMinimap;
    }

    [RelayCommand]
    public void ToggleSidePanel()
    {
        IsSidePanelVisible = !IsSidePanelVisible;
    }

    partial void OnZoomLevelChanged(double value)
    {
        _settingsService.Settings.ZoomLevel = value;
    }
}
