using Avalonia.Controls;
using Avalonia.Input;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Dialogs;

public partial class FindReplacePanel : UserControl
{
    public FindReplacePanel()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not FindReplaceViewModel vm) return;

        if (e.Key == Key.Escape)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            vm.FindNextCommand.Execute(null);
            e.Handled = true;
        }
    }
}
