using Avalonia.Controls;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class EditToolbar : UserControl
{
    public EditToolbar()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ShellViewModel mainVm)
        {
            var transformService = new TextTransformService();
            var commentService = new CommentService();
            DataContext = new EditToolbarViewModel(transformService, commentService, mainVm);
        }
    }
}
