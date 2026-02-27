using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.Views.Components;
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

        if (DataContext is ShellViewModel vm)
        {
            // Subscribe to sub-VM property changes
            vm.Settings.PropertyChanged += OnSettingsPropertyChanged;
            vm.TabManager.PropertyChanged += OnTabManagerPropertyChanged;
            vm.PropertyChanged += OnShellPropertyChanged;

            // Subscribe to file watcher events
            vm.FileChangedExternally += OnFileChangedExternally;

            // Subscribe to search in files
            vm.ShowSearchInFilesRequested += OnShowSearchInFiles;

            // Subscribe to method search
            vm.ShowMethodSearchRequested += OnShowMethodSearch;

            // Restore session
            Dispatcher.UIThread.Post(async () =>
            {
                await vm.RestoreSession();
            });
        }
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;

        if (e.PropertyName == nameof(EditorSettingsViewModel.IsFullScreen))
        {
            if (vm.Settings.IsFullScreen)
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
        else if (e.PropertyName == nameof(EditorSettingsViewModel.CurrentTheme))
        {
            Avalonia.Application.Current!.RequestedThemeVariant = vm.Settings.CurrentTheme == "Dark"
                ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        }
    }

    private void OnTabManagerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;

        if (e.PropertyName == nameof(TabManagerViewModel.IsSplitViewActive))
        {
            UpdateSplitViewLayout(vm.TabManager.IsSplitViewActive);
        }
        else if (e.PropertyName == nameof(TabManagerViewModel.ActiveTab))
        {
            UpdateMarkdownPreview();
        }
    }

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.IsMarkdownPreviewVisible))
        {
            UpdateMarkdownPreview();
        }
    }

    private void UpdateSplitViewLayout(bool isSplit)
    {
        var editorGrid = this.FindControl<Grid>("EditorGrid");
        var primaryEditor = this.FindControl<EditorControl>("PrimaryEditor");
        var secondaryEditor = this.FindControl<EditorControl>("SecondaryEditor");

        if (editorGrid == null || primaryEditor == null || secondaryEditor == null) return;

        if (isSplit)
        {
            // Set up split columns: *, Auto(splitter), *
            editorGrid.ColumnDefinitions.Clear();
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            Grid.SetColumn(primaryEditor, 0);

            // Add a GridSplitter between the two editors
            var splitter = new GridSplitter
            {
                Width = 4,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E0E0E0"))
            };
            splitter.Name = "EditorSplitter";
            Grid.SetColumn(splitter, 1);

            // Remove any existing splitter
            var existingSplitter = editorGrid.Children.OfType<GridSplitter>().FirstOrDefault();
            if (existingSplitter != null)
                editorGrid.Children.Remove(existingSplitter);

            editorGrid.Children.Add(splitter);
            Grid.SetColumn(secondaryEditor, 2);
        }
        else
        {
            // Remove splitter
            var existingSplitter = editorGrid.Children.OfType<GridSplitter>().FirstOrDefault();
            if (existingSplitter != null)
                editorGrid.Children.Remove(existingSplitter);

            // Reset to single column
            editorGrid.ColumnDefinitions.Clear();
            editorGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            Grid.SetColumn(primaryEditor, 0);
            Grid.SetColumn(secondaryEditor, 0);
        }
    }

    private void UpdateMarkdownPreview()
    {
        if (DataContext is not ShellViewModel vm) return;
        if (!vm.IsMarkdownPreviewVisible) return;

        var preview = this.FindControl<MarkdownPreviewPanel>("MarkdownPreview");
        if (preview == null) return;

        if (vm.TabManager.ActiveTab?.Language == SupportedLanguage.Markdown)
        {
            preview.UpdatePreview(vm.TabManager.ActiveTab.Content ?? string.Empty);

            // Wire for live updates
            vm.TabManager.ActiveTab.PropertyChanged -= OnActiveTabContentChangedForPreview;
            vm.TabManager.ActiveTab.PropertyChanged += OnActiveTabContentChangedForPreview;
        }
        else
        {
            preview.ClearPreview();
        }
    }

    private void OnActiveTabContentChangedForPreview(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DocumentTabViewModel.Content)) return;
        if (DataContext is not ShellViewModel vm) return;
        if (!vm.IsMarkdownPreviewVisible) return;

        var preview = this.FindControl<MarkdownPreviewPanel>("MarkdownPreview");
        if (preview == null || vm.TabManager.ActiveTab == null) return;

        if (vm.TabManager.ActiveTab.Language == SupportedLanguage.Markdown)
            preview.UpdatePreview(vm.TabManager.ActiveTab.Content ?? string.Empty);
    }

    private async void OnFileChangedExternally(string filePath, string message)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (DataContext is not ShellViewModel vm) return;

            IDialogService? dialogService = null;
            try { dialogService = App.Services.GetService<IDialogService>(); } catch { }

            if (dialogService != null)
            {
                await dialogService.ShowFileChangedDialogAsync(message, () => vm.ReloadFile(filePath));
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
        if (DataContext is not ShellViewModel vm) return;
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
            DataContext is ShellViewModel vm)
        {
            vm.TabManager.ActiveTab = tab;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_forceClose) return;
        if (DataContext is not ShellViewModel vm) return;

        // Save session before closing
        vm.SaveSession();

        var modifiedTabs = vm.TabManager.Tabs.Where(t => t.IsModified).ToList();
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

        if (DataContext is not ShellViewModel vm) return;

        // Split view: Ctrl+\ (OemBackslash / OemPipe)
        if (e.KeyModifiers == KeyModifiers.Control && (e.Key == Key.OemBackslash || e.Key == Key.OemPipe))
        {
            vm.ToggleSplitViewCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Terminal: Ctrl+` (backtick / OemTilde)
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.OemTilde)
        {
            vm.ToggleTerminalCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Select word and highlight: Ctrl+D
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D)
        {
            vm.SelectWordAndHighlightCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Search in files: Ctrl+Shift+F
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.F)
        {
            vm.ShowSearchInFilesCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Method search: Ctrl+Shift+M
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.M)
        {
            vm.ShowMethodSearchCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Duplicate line: Ctrl+Shift+D
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.D)
        {
            vm.DuplicateLineCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Delete line: Ctrl+Shift+K
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.K)
        {
            vm.DeleteLineCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Move line up: Alt+Up
        if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Up)
        {
            vm.MoveLineUpCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Move line down: Alt+Down
        if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Down)
        {
            vm.MoveLineDownCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Go to matching bracket: Ctrl+B
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.B)
        {
            vm.GoToMatchingBracketCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Clipboard history: Ctrl+Shift+V
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.V)
        {
            ShowClipboardHistoryPopup(vm);
            e.Handled = true;
            return;
        }

        // Command palette: Ctrl+Shift+P
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.P)
        {
            vm.ShowCommandPaletteCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Escape closes command palette
        if (e.Key == Key.Escape && vm.CommandPalette.IsVisible)
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

    private void OnShowSearchInFiles()
    {
        // Find the SidePanel in the visual tree and switch to search tab
        var sidePanel = this.GetVisualDescendants().OfType<SidePanel>().FirstOrDefault();
        sidePanel?.ShowSearchTab();
    }

    private void OnShowMethodSearch()
    {
        // Find the SidePanel in the visual tree and switch to methods tab
        var sidePanel = this.GetVisualDescendants().OfType<SidePanel>().FirstOrDefault();
        sidePanel?.ShowMethodsTab();
    }


    // Clipboard history popup
    private void ShowClipboardHistoryPopup(ShellViewModel vm)
    {
        if (vm.Clipboard.History.Count == 0) return;

        var popup = new Window
        {
            Title = "Historique presse-papiers",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var listBox = new ListBox();
        foreach (var item in vm.Clipboard.History)
        {
            var display = item.Length > 80 ? item[..80] + "..." : item;
            display = display.Replace("\r\n", " ").Replace("\n", " ");
            listBox.Items.Add(new ListBoxItem { Content = display, Tag = item });
        }

        listBox.DoubleTapped += async (_, _) =>
        {
            if (listBox.SelectedItem is ListBoxItem { Tag: string text })
            {
                var clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(text);
                vm.PasteCommand.Execute(null);
                popup.Close();
            }
        };

        popup.Content = new DockPanel
        {
            Margin = new Thickness(8),
            Children =
            {
                listBox
            }
        };

        popup.ShowDialog(this);
    }

    private async Task ShowGoToLineDialog(ShellViewModel vm)
    {
        if (vm.TabManager.ActiveTab == null) return;

        var lineCount = vm.TabManager.ActiveTab.Content.Split('\n').Length;
        var dialog = new GoToLineDialog(lineCount);
        await dialog.ShowDialog(this);

        if (dialog.SelectedLine.HasValue)
        {
            vm.TabManager.ActiveTab.CursorLine = dialog.SelectedLine.Value;
        }
    }
}
