using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NotepadCommander.UI.Views.Dialogs;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var versionText = this.FindControl<TextBlock>("VersionText");
        if (versionText != null)
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            versionText.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
        }

        var btn = this.FindControl<Button>("CloseButton");
        if (btn != null) btn.Click += (_, _) => Close();
    }
}
