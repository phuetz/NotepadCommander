using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NotepadCommander.UI.Views.Dialogs;

public partial class GoToLineDialog : Window
{
    public int? SelectedLine { get; private set; }

    public GoToLineDialog()
    {
        InitializeComponent();
    }

    public GoToLineDialog(int maxLine) : this()
    {
        var box = this.FindControl<TextBox>("LineNumberBox");
        if (box != null)
        {
            box.Watermark = $"1 - {maxLine}";
        }
    }

    private void OnGoClicked(object? sender, RoutedEventArgs e)
    {
        var box = this.FindControl<TextBox>("LineNumberBox");
        if (box != null && int.TryParse(box.Text, out var line) && line > 0)
        {
            SelectedLine = line;
            Close(true);
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
