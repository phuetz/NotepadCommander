using CommunityToolkit.Mvvm.ComponentModel;
using NotepadCommander.Core.Services;

namespace NotepadCommander.UI.ViewModels.Dialogs;

public partial class SettingsDialogViewModel : ModalDialogViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private double fontSize;
    [ObservableProperty] private string fontFamily = "Cascadia Code";
    [ObservableProperty] private bool showLineNumbers;
    [ObservableProperty] private bool wordWrap;
    [ObservableProperty] private bool highlightCurrentLine;
    [ObservableProperty] private bool showWhitespace;
    [ObservableProperty] private bool autoSave;
    [ObservableProperty] private int autoSaveIntervalSeconds;
    [ObservableProperty] private int themeIndex; // 0 = Light, 1 = Dark

    /// <summary>
    /// Raised after settings are saved, so caller can sync to EditorSettingsViewModel.
    /// </summary>
    public event Action? SettingsSaved;

    public SettingsDialogViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
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
        SettingsSaved?.Invoke();
    }

    public void ResetSettings()
    {
        _settingsService.Reset();
        LoadSettings();
    }
}
