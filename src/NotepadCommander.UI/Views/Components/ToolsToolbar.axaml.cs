using Avalonia.Controls;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class ToolsToolbar : UserControl
{
    public ToolsToolbar()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel mainVm)
        {
            DataContext = new ToolsToolbarViewModel(mainVm);
        }
    }
}
