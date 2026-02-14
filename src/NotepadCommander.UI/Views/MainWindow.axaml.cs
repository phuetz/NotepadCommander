using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.Views.Dialogs;

namespace NotepadCommander.UI.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnFileDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DragDrop.SetAllowDrop(this, true);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnFileDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (!e.Data.Contains(DataFormats.Files)) return;

        var files = e.Data.GetFiles();
        if (files == null) return;

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (File.Exists(path))
            {
                await vm.OpenFilePath(path);
            }
        }
    }

    private void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DocumentTabViewModel tab } &&
            DataContext is MainWindowViewModel vm)
        {
            vm.ActiveTab = tab;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_forceClose) return;
        if (DataContext is not MainWindowViewModel vm) return;

        var modifiedTabs = vm.Tabs.Where(t => t.IsModified).ToList();
        if (modifiedTabs.Count == 0) return;

        // Cancel the close, handle async save, then re-close
        e.Cancel = true;

        var dialog = new SavePromptDialog();
        await dialog.ShowDialog(this);

        if (dialog.Result == SavePromptResult.Cancel)
            return;

        if (dialog.Result == SavePromptResult.Save)
        {
            await vm.SaveAllFilesCommand.ExecuteAsync(this);
        }

        _forceClose = true;
        Close();
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not MainWindowViewModel vm) return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            var num = e.Key switch
            {
                Key.D1 => 1,
                Key.D2 => 2,
                Key.D3 => 3,
                Key.D4 => 4,
                Key.D5 => 5,
                _ => 0
            };

            if (num > 0)
            {
                vm.ToolbarViewModel.SelectTabByNumber(num);
                e.Handled = true;
                return;
            }
        }

        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.G)
        {
            await ShowGoToLineDialog(vm);
            e.Handled = true;
        }
    }

    private async Task ShowGoToLineDialog(MainWindowViewModel vm)
    {
        if (vm.ActiveTab == null) return;

        var lineCount = vm.ActiveTab.Content.Split('\n').Length;
        var dialog = new GoToLineDialog(lineCount);
        await dialog.ShowDialog(this);

        if (dialog.SelectedLine.HasValue)
        {
            vm.ActiveTab.CursorLine = dialog.SelectedLine.Value;
        }
    }
}
