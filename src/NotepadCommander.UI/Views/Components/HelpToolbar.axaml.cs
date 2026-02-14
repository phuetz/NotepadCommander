using Avalonia.Controls;
using Avalonia.Interactivity;
using NotepadCommander.UI.Views.Dialogs;

namespace NotepadCommander.UI.Views.Components;

public partial class HelpToolbar : UserControl
{
    public HelpToolbar()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var aboutBtn = this.FindControl<Button>("AboutButton");
        var shortcutsBtn = this.FindControl<Button>("ShortcutsButton");
        var settingsBtn = this.FindControl<Button>("SettingsButton");

        if (aboutBtn != null)
            aboutBtn.Click += async (_, _) =>
            {
                var dialog = new AboutDialog();
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window != null) await dialog.ShowDialog(window);
            };

        if (shortcutsBtn != null)
            shortcutsBtn.Click += async (_, _) =>
            {
                var dialog = new ShortcutsDialog();
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window != null) await dialog.ShowDialog(window);
            };

        if (settingsBtn != null)
            settingsBtn.Click += async (_, _) =>
            {
                var dialog = new SettingsDialog();
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window != null) await dialog.ShowDialog(window);
            };
    }
}
