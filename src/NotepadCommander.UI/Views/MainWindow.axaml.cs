using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.Views.Dialogs;

namespace NotepadCommander.UI.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;
    private WindowState _previousWindowState;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnFileDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DragDrop.SetAllowDrop(this, true);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainWindowViewModel vm)
        {
            // Subscribe to fullscreen toggle
            vm.PropertyChanged += OnViewModelPropertyChanged;

            // Subscribe to file watcher events
            vm.FileChangedExternally += OnFileChangedExternally;

            // Restore session
            Dispatcher.UIThread.Post(async () =>
            {
                await vm.RestoreSession();
            });
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsFullScreen) &&
            DataContext is MainWindowViewModel vm)
        {
            if (vm.IsFullScreen)
            {
                _previousWindowState = WindowState;
                WindowState = WindowState.FullScreen;
            }
            else
            {
                WindowState = _previousWindowState == WindowState.FullScreen
                    ? WindowState.Normal
                    : _previousWindowState;
            }
        }
    }

    private async void OnFileChangedExternally(string filePath, string message)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (DataContext is not MainWindowViewModel vm) return;

            var dialog = new Window
            {
                Title = "Fichier modifie",
                Width = 450,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var result = false;
            var yesBtn = new Button { Content = "Recharger", Margin = new Thickness(0, 0, 8, 0) };
            var noBtn = new Button { Content = "Ignorer" };

            yesBtn.Click += (_, _) => { result = true; dialog.Close(); };
            noBtn.Click += (_, _) => { result = false; dialog.Close(); };

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children = { yesBtn, noBtn }
                    }
                }
            };

            await dialog.ShowDialog(this);

            if (result)
            {
                await vm.ReloadFile(filePath);
            }
        });
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

        // Save session before closing
        vm.SaveSession();

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

        // Command palette: Ctrl+Shift+P
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.P)
        {
            vm.ShowCommandPaletteCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Escape closes command palette
        if (e.Key == Key.Escape && vm.IsCommandPaletteVisible)
        {
            vm.HideCommandPaletteCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Quick open: Ctrl+P
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.P)
        {
            vm.ShowCommandPaletteCommand.Execute(null);
            e.Handled = true;
            return;
        }

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
