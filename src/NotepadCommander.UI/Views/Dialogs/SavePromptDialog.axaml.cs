using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NotepadCommander.UI.Views.Dialogs;

public enum SavePromptResult
{
    Save,
    DontSave,
    Cancel
}

public partial class SavePromptDialog : Window
{
    public SavePromptResult Result { get; private set; } = SavePromptResult.Cancel;

    public SavePromptDialog()
    {
        InitializeComponent();
    }

    public SavePromptDialog(string fileName) : this()
    {
        var text = this.FindControl<TextBlock>("FileNameText");
        if (text != null)
        {
            text.Text = $"Le fichier \"{fileName}\" a ete modifie.";
        }
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        Result = SavePromptResult.Save;
        Close(Result);
    }

    private void OnDontSaveClicked(object? sender, RoutedEventArgs e)
    {
        Result = SavePromptResult.DontSave;
        Close(Result);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Result = SavePromptResult.Cancel;
        Close(Result);
    }
}
