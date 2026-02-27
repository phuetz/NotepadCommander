using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.UI.Models;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.ViewModels.Tools;

namespace NotepadCommander.UI.Views.Components;

public partial class SearchPanel : UserControl
{
    private SearchToolViewModel? _vm;
    private bool _servicesResolved;

    public SearchPanel()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_servicesResolved) return;
        _servicesResolved = true;

        try
        {
            _vm = App.Services.GetRequiredService<SearchToolViewModel>();
            DataContext = _vm;

            _vm.NavigateToFileRequested += OnNavigateToFile;
        }
        catch { }
    }

    public void SetSearchDirectory(string? directory)
    {
        if (_vm != null)
            _vm.SearchRootDirectory = directory;
    }

    public void FocusSearchBox()
    {
        var box = this.FindControl<TextBox>("SearchPatternBox");
        box?.Focus();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _vm?.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void OnReplaceAllClick(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var window = TopLevel.GetTopLevel(this);
        var tabManager = (window?.DataContext as ShellViewModel)?.TabManager;
        await _vm.ReplaceAll(tabManager);
    }

    private async void OnResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView tree) return;
        var selected = tree.SelectedItem;

        string? filePath = null;
        int line = 1;

        if (selected is SearchMatchItem match)
        {
            filePath = match.FilePath;
            line = match.Line;
        }
        else if (selected is SearchFileGroup group)
        {
            filePath = group.FilePath;
        }

        if (filePath == null) return;

        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is ShellViewModel vm)
        {
            await vm.OpenFilePath(filePath);
            if (vm.TabManager.ActiveTab != null)
                vm.TabManager.ActiveTab.CursorLine = line;
        }
    }

    private async void OnNavigateToFile(string filePath, int line)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is ShellViewModel vm)
        {
            await vm.OpenFilePath(filePath);
            if (vm.TabManager.ActiveTab != null)
                vm.TabManager.ActiveTab.CursorLine = line;
        }
    }
}
