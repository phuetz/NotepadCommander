using Avalonia.Controls;
using Avalonia.Interactivity;
using NotepadCommander.UI.Views.Dialogs;

namespace NotepadCommander.UI.Views.Components;

public partial class HelpToolbar : UserControl
{
    private bool _handlersAttached;

    public HelpToolbar()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_handlersAttached) return;
        _handlersAttached = true;

        var aboutBtn = this.FindControl<Button>("AboutButton");
        var shortcutsBtn = this.FindControl<Button>("ShortcutsButton");
        var settingsBtn = this.FindControl<Button>("SettingsButton");

        if (aboutBtn != null)
            aboutBtn.Click += OnAboutClick;

        if (shortcutsBtn != null)
            shortcutsBtn.Click += OnShortcutsClick;

        if (settingsBtn != null)
            settingsBtn.Click += OnSettingsClick;
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog();
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null) await dialog.ShowDialog(window);
    }

    private async void OnShortcutsClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ShortcutsDialog();
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null) await dialog.ShowDialog(window);
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog();
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null) await dialog.ShowDialog(window);
    }
}
