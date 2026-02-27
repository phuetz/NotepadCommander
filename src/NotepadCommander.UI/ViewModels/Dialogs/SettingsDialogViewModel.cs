using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Services;
using NotepadCommander.UI.Services;

namespace NotepadCommander.UI.ViewModels.Dialogs;

public partial class SettingsDialogViewModel : ModalDialogViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly EditorThemeService _editorThemeService;

    [ObservableProperty] private double fontSize;
    [ObservableProperty] private string fontFamily = "Cascadia Code";
    [ObservableProperty] private bool showLineNumbers;
    [ObservableProperty] private bool wordWrap;
    [ObservableProperty] private bool highlightCurrentLine;
    [ObservableProperty] private bool showWhitespace;
    [ObservableProperty] private bool autoSave;
    [ObservableProperty] private int autoSaveIntervalSeconds;
    [ObservableProperty] private int themeIndex; // 0 = Light, 1 = Dark

    // Editor theme properties
    [ObservableProperty] private string selectedEditorTheme = "Light Plus";
    [ObservableProperty] private bool followChromeTheme = true;

    public string[] AvailableEditorThemes => EditorThemeService.AvailableThemes;

    /// <summary>
    /// Raised after settings are saved, so caller can sync to EditorSettingsViewModel.
    /// </summary>
    public event Action? SettingsSaved;

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;

    public SettingsDialogViewModel(ISettingsService settingsService, EditorThemeService editorThemeService)
    {
        _settingsService = settingsService;
        _editorThemeService = editorThemeService;
        DialogTitle = "Parametres";
        OkButtonText = "Enregistrer";

        LoadSettings();
    }

    public void LoadSettings()
    {
        var s = _settingsService.Settings;
        FontSize = s.FontSize;
        FontFamily = s.FontFamily;
        ShowLineNumbers = s.ShowLineNumbers;
        WordWrap = s.WordWrap;
        HighlightCurrentLine = s.HighlightCurrentLine;
        ShowWhitespace = s.ShowWhitespace;
        AutoSave = s.AutoSave;
        AutoSaveIntervalSeconds = s.AutoSaveIntervalSeconds;
        ThemeIndex = s.Theme == "Dark" ? 1 : 0;

        SelectedEditorTheme = _editorThemeService.CurrentEditorTheme;
        FollowChromeTheme = _editorThemeService.FollowChromeTheme;
    }

    [RelayCommand]
    public void Save()
    {
        SaveSettings();
        CloseRequested?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanCloseDialog))]
    public void CloseDialog()
    {
        CloseRequested?.Invoke();
    }

    private bool CanCloseDialog() => true;

    [RelayCommand]
    public void Reset()
    {
        ResetSettings();
    }

    public void SaveSettings()
    {
        var s = _settingsService.Settings;
        s.FontSize = FontSize;
        s.FontFamily = FontFamily;
        s.ShowLineNumbers = ShowLineNumbers;
        s.WordWrap = WordWrap;
        s.HighlightCurrentLine = HighlightCurrentLine;
        s.ShowWhitespace = ShowWhitespace;
        s.AutoSave = AutoSave;
        s.AutoSaveIntervalSeconds = AutoSaveIntervalSeconds;
        s.Theme = ThemeIndex == 1 ? "Dark" : "Light";

        _settingsService.Save();

        // Apply editor theme
        _editorThemeService.FollowChromeTheme = FollowChromeTheme;
        if (!FollowChromeTheme)
            _editorThemeService.SetEditorTheme(SelectedEditorTheme);
        else
            _editorThemeService.SyncWithChromeTheme(s.Theme);

        SettingsSaved?.Invoke();
    }

    public void ResetSettings()
    {
        _settingsService.Reset();
        LoadSettings();
    }
}
