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
        var btn = this.FindControl<Button>("CloseButton");
        if (btn != null) btn.Click += (_, _) => Close();
    }
}
