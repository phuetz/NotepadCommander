using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services;

namespace NotepadCommander.UI.Views.Dialogs;

public partial class SettingsDialog : Window
{
    private readonly ISettingsService _settingsService;

    public SettingsDialog()
    {
        InitializeComponent();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        LoadSettings();

        var saveBtn = this.FindControl<Button>("SaveButton");
        var cancelBtn = this.FindControl<Button>("CancelButton");
        var resetBtn = this.FindControl<Button>("ResetButton");

        if (saveBtn != null) saveBtn.Click += (_, _) => { SaveSettings(); Close(); };
        if (cancelBtn != null) cancelBtn.Click += (_, _) => Close();
        if (resetBtn != null) resetBtn.Click += (_, _) => { _settingsService.Reset(); LoadSettings(); };
    }

    private void LoadSettings()
    {
        var s = _settingsService.Settings;
        var fontSize = this.FindControl<NumericUpDown>("FontSizeInput");
        var fontFamily = this.FindControl<TextBox>("FontFamilyInput");
        var lineNumbers = this.FindControl<CheckBox>("ShowLineNumbersCheck");
        var wordWrap = this.FindControl<CheckBox>("WordWrapCheck");
        var highlightLine = this.FindControl<CheckBox>("HighlightLineCheck");
        var showWhitespace = this.FindControl<CheckBox>("ShowWhitespaceCheck");
        var autoSave = this.FindControl<CheckBox>("AutoSaveCheck");
        var autoSaveInterval = this.FindControl<NumericUpDown>("AutoSaveIntervalInput");
        var theme = this.FindControl<ComboBox>("ThemeCombo");

        if (fontSize != null) fontSize.Value = (decimal)s.FontSize;
        if (fontFamily != null) fontFamily.Text = s.FontFamily;
        if (lineNumbers != null) lineNumbers.IsChecked = s.ShowLineNumbers;
        if (wordWrap != null) wordWrap.IsChecked = s.WordWrap;
        if (highlightLine != null) highlightLine.IsChecked = s.HighlightCurrentLine;
        if (showWhitespace != null) showWhitespace.IsChecked = s.ShowWhitespace;
        if (autoSave != null) autoSave.IsChecked = s.AutoSave;
        if (autoSaveInterval != null) autoSaveInterval.Value = s.AutoSaveIntervalSeconds;
        if (theme != null) theme.SelectedIndex = s.Theme == "Dark" ? 1 : 0;
    }

    private void SaveSettings()
    {
        var s = _settingsService.Settings;
        var fontSize = this.FindControl<NumericUpDown>("FontSizeInput");
        var fontFamily = this.FindControl<TextBox>("FontFamilyInput");
        var lineNumbers = this.FindControl<CheckBox>("ShowLineNumbersCheck");
        var wordWrap = this.FindControl<CheckBox>("WordWrapCheck");
        var highlightLine = this.FindControl<CheckBox>("HighlightLineCheck");
        var showWhitespace = this.FindControl<CheckBox>("ShowWhitespaceCheck");
        var autoSave = this.FindControl<CheckBox>("AutoSaveCheck");
        var autoSaveInterval = this.FindControl<NumericUpDown>("AutoSaveIntervalInput");
        var theme = this.FindControl<ComboBox>("ThemeCombo");

        if (fontSize?.Value != null) s.FontSize = (double)fontSize.Value;
        if (fontFamily != null) s.FontFamily = fontFamily.Text ?? "Cascadia Code";
        if (lineNumbers != null) s.ShowLineNumbers = lineNumbers.IsChecked ?? true;
        if (wordWrap != null) s.WordWrap = wordWrap.IsChecked ?? false;
        if (highlightLine != null) s.HighlightCurrentLine = highlightLine.IsChecked ?? true;
        if (showWhitespace != null) s.ShowWhitespace = showWhitespace.IsChecked ?? false;
        if (autoSave != null) s.AutoSave = autoSave.IsChecked ?? false;
        if (autoSaveInterval?.Value != null) s.AutoSaveIntervalSeconds = (int)autoSaveInterval.Value;
        if (theme != null) s.Theme = theme.SelectedIndex == 1 ? "Dark" : "Light";

        _settingsService.Save();

        // Apply settings to MainWindowViewModel if available
        if (Owner is Window { DataContext: ViewModels.MainWindowViewModel mainVm })
        {
            mainVm.ShowLineNumbers = s.ShowLineNumbers;
            mainVm.WordWrap = s.WordWrap;
            mainVm.HighlightCurrentLine = s.HighlightCurrentLine;
            mainVm.ShowWhitespace = s.ShowWhitespace;
            if (s.Theme == "Dark") mainVm.SetThemeDarkCommand.Execute(null);
            else mainVm.SetThemeLightCommand.Execute(null);
        }
    }
}
