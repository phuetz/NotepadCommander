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
    private DocumentTabViewModel? _draggedTab;

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

        // Wire TabBar DragDrop at the container level
        var tabBar = this.FindControl<ItemsControl>("TabBar");
        if (tabBar != null)
        {
            tabBar.AddHandler(DragDrop.DropEvent, OnTabBarDrop);
            tabBar.AddHandler(DragDrop.DragOverEvent, OnTabDragOver);
        }

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

            // Subscribe to clipboard paste (inline overlay)
            vm.ClipboardPasteRequested += OnClipboardPasteRequested;

            // Subscribe to copy-to-clipboard
            vm.CopyToClipboardRequested += OnCopyToClipboardRequested;

            // Auto-focus command palette when visible
            vm.CommandPalette.PropertyChanged += (_, pe) =>
            {
                if (pe.PropertyName == nameof(CommandPaletteViewModel.IsVisible) && vm.CommandPalette.IsVisible)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var input = this.FindControl<TextBox>("CommandPaletteInput");
                        input?.Focus();
                    }, DispatcherPriority.Input);
                }
            };

            // Auto-focus clipboard filter when visible
            vm.Clipboard.PropertyChanged += (_, pe) =>
            {
                if (pe.PropertyName == nameof(ClipboardHistoryViewModel.IsVisible) && vm.Clipboard.IsVisible)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var input = this.FindControl<TextBox>("ClipboardFilterInput");
                        input?.Focus();
                    }, DispatcherPriority.Input);
                }
            };

            // Auto-focus go-to-line input when visible
            vm.PropertyChanged += (_, pe) =>
            {
                if (pe.PropertyName == nameof(ShellViewModel.IsGoToLineVisible) && vm.IsGoToLineVisible)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var input = this.FindControl<TextBox>("GoToLineInput");
                        input?.Focus();
                    }, DispatcherPriority.Input);
                }
            };

            // Restore session
            Dispatcher.UIThread.Post(async () =>
            {
                await vm.RestoreSession();
            });
        }
    }

    private async void OnClipboardPasteRequested(string text)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(text);
        if (DataContext is ShellViewModel vm)
            vm.PasteCommand.Execute(null);
    }

    private async void OnCopyToClipboardRequested(string text)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(text);
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
            UpdateMarkdownScrollSync();
        }
    }

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.IsMarkdownPreviewVisible))
        {
            UpdateMarkdownPreview();
            UpdateMarkdownScrollSync();
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

    private void UpdateMarkdownScrollSync()
    {
        if (DataContext is not ShellViewModel vm) return;
        if (!vm.IsMarkdownPreviewVisible) return;

        // Scroll sync is handled via editor scroll events → MarkdownPreview.ScrollToRatio
        var primaryEditor = this.FindControl<EditorControl>("PrimaryEditor");
        var preview = this.FindControl<MarkdownPreviewPanel>("MarkdownPreview");
        if (primaryEditor == null || preview == null) return;

        // Wire primary editor scroll to markdown preview
        primaryEditor.EditorScrollChanged -= OnEditorScrollForMarkdown;
        primaryEditor.EditorScrollChanged += OnEditorScrollForMarkdown;
    }

    private void OnEditorScrollForMarkdown(double ratio)
    {
        var preview = this.FindControl<MarkdownPreviewPanel>("MarkdownPreview");
        preview?.ScrollToRatio(ratio);
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

    // Tab drag-drop reordering
    private async void OnTabPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not Button { Tag: DocumentTabViewModel tab }) return;
        if (DataContext is not ShellViewModel vm) return;

        // Only start drag on left mouse button with a small move threshold
        if (!e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed) return;

        _draggedTab = tab;
        vm.TabManager.ActiveTab = tab;

        var data = new DataObject();
        data.Set("NotepadCommanderTab", tab);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        _draggedTab = null;
    }

    private void OnTabDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("NotepadCommanderTab"))
            e.DragEffects = DragDropEffects.Move;
        else
            e.DragEffects = DragDropEffects.None;
    }

    private void OnTabBarDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;
        if (_draggedTab == null) return;

        // Find target button at drop position
        var tabBar = this.FindControl<ItemsControl>("TabBar");
        if (tabBar == null) return;

        var pos = e.GetPosition(tabBar);
        var targetBtn = tabBar.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.Tag is DocumentTabViewModel && b.Bounds.Contains(pos));

        if (targetBtn?.Tag is DocumentTabViewModel targetTab && targetTab != _draggedTab)
        {
            var fromIndex = vm.TabManager.Tabs.IndexOf(_draggedTab);
            var toIndex = vm.TabManager.Tabs.IndexOf(targetTab);
            if (fromIndex >= 0 && toIndex >= 0)
                vm.TabManager.MoveTab(fromIndex, toIndex);
        }
    }

    // Keep unused stub to avoid breaking any potential reflection references
    private void OnTabDrop(object? sender, DragEventArgs e) { }

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

    protected override void OnKeyDown(KeyEventArgs e)
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
            vm.ShowClipboardHistoryCommand.Execute(null);
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

        // Escape closes overlays
        if (e.Key == Key.Escape)
        {
            if (vm.CommandPalette.IsVisible)
            {
                vm.HideCommandPaletteCommand.Execute(null);
                e.Handled = true;
                return;
            }
            if (vm.Clipboard.IsVisible)
            {
                vm.Clipboard.Hide();
                e.Handled = true;
                return;
            }
            if (vm.IsGoToLineVisible)
            {
                vm.HideGoToLineCommand.Execute(null);
                e.Handled = true;
                return;
            }
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

        // Go-to-line: Ctrl+G
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.G)
        {
            vm.ShowGoToLineCommand.Execute(null);
            e.Handled = true;
        }
    }

    // Command palette keyboard navigation
    private void OnCommandPaletteKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;

        if (e.Key == Key.Up)
        {
            vm.CommandPalette.MoveUp();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            vm.CommandPalette.MoveDown();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            vm.CommandPalette.Confirm();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CommandPalette.Hide();
            e.Handled = true;
        }
    }

    private void OnCommandPaletteDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;
        vm.CommandPalette.Confirm();
    }

    // Clipboard history keyboard navigation
    private void OnClipboardHistoryKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;

        if (e.Key == Key.Up)
        {
            vm.Clipboard.MoveUp();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            vm.Clipboard.MoveDown();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            vm.Clipboard.SelectCurrent();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.Clipboard.Hide();
            e.Handled = true;
        }
    }

    private void OnClipboardHistoryDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;
        vm.Clipboard.SelectCurrent();
    }

    // Go-to-line keyboard handler
    private void OnGoToLineKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            vm.ConfirmGoToLineCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.HideGoToLineCommand.Execute(null);
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
}
