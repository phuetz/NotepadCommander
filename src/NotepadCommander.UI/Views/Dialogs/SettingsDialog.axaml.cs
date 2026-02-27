using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels.Dialogs;

namespace NotepadCommander.UI.Views.Dialogs;

public partial class SettingsDialog : Window
{
    private readonly SettingsDialogViewModel _vm;

    public SettingsDialog() : this(
        App.Services.GetRequiredService<ISettingsService>(),
        App.Services.GetRequiredService<EditorThemeService>())
    {
    }

    public SettingsDialog(ISettingsService settingsService, EditorThemeService editorThemeService)
    {
        InitializeComponent();
        _vm = new SettingsDialogViewModel(settingsService, editorThemeService);
        DataContext = _vm;

        _vm.SettingsSaved += OnSettingsSaved;
        _vm.CloseRequested += () => Close();
    }

    private void OnSettingsSaved()
    {
        if (Owner is Window { DataContext: ViewModels.ShellViewModel shellVm })
        {
            var s = _vm;
            shellVm.Settings.ShowLineNumbers = s.ShowLineNumbers;
            shellVm.Settings.WordWrap = s.WordWrap;
            shellVm.Settings.HighlightCurrentLine = s.HighlightCurrentLine;
            shellVm.Settings.ShowWhitespace = s.ShowWhitespace;
            if (s.ThemeIndex == 1) shellVm.SetThemeDarkCommand.Execute(null);
            else shellVm.SetThemeLightCommand.Execute(null);
        }
    }
}
