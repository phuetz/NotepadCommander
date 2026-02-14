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

        if (e.Key == Key.Escape && DataContext is FindReplaceViewModel vm)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && DataContext is FindReplaceViewModel vm2)
        {
            vm2.FindNextCommand.Execute(null);
            e.Handled = true;
        }
    }
}
