using Avalonia.Controls;
using Avalonia.Input;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class ToolbarTabControl : UserControl
{
    private Border? _backstagePanel;

    public ToolbarTabControl()
    {
        InitializeComponent();
        _backstagePanel = this.FindControl<Border>("BackstagePanel");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && DataContext is ToolbarViewModel vm && vm.IsFileBackstageOpen)
        {
            vm.CloseFileBackstageCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnBackstageOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not ToolbarViewModel vm || !vm.IsFileBackstageOpen)
            return;

        if (_backstagePanel == null)
        {
            vm.CloseFileBackstageCommand.Execute(null);
            e.Handled = true;
            return;
        }

        var point = e.GetPosition(_backstagePanel);
        var isInsidePanel = point.X >= 0 &&
                            point.Y >= 0 &&
                            point.X <= _backstagePanel.Bounds.Width &&
                            point.Y <= _backstagePanel.Bounds.Height;

        if (!isInsidePanel)
        {
            vm.CloseFileBackstageCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnBackstageActionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ToolbarViewModel vm)
        {
            vm.CloseFileBackstageCommand.Execute(null);
        }
    }
}
