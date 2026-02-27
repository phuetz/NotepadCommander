using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Snippets;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class SnippetManagerPanel : UserControl
{
    private ISnippetService? _snippetService;
    private IDialogService? _dialogService;
    private List<Snippet> _snippets = new();
    private bool _servicesResolved;
    private string _filterText = string.Empty;
    private string _selectedCategory = "";

    public SnippetManagerPanel()
    {
        InitializeComponent();

        var filterBox = this.FindControl<TextBox>("FilterBox");
        if (filterBox != null)
            filterBox.TextChanged += (_, _) =>
            {
                _filterText = filterBox.Text ?? string.Empty;
                RefreshList();
            };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (!_servicesResolved)
        {
            _servicesResolved = true;
            try
            {
                _snippetService = App.Services.GetService<ISnippetService>();
                _dialogService = App.Services.GetService<IDialogService>();
            }
            catch { }
        }
        RefreshCategories();
        RefreshList();
    }

    private void RefreshCategories()
    {
        if (_snippetService == null) return;

        var combo = this.FindControl<ComboBox>("CategoryFilter");
        if (combo == null) return;

        var categories = _snippetService.GetCategories();
        combo.Items.Clear();
        combo.Items.Add(new ComboBoxItem { Content = "Toutes les categories", Tag = "" });
        foreach (var cat in categories)
            combo.Items.Add(new ComboBoxItem { Content = cat, Tag = cat });

        combo.SelectedIndex = 0;
    }

    private void RefreshList()
    {
        if (_snippetService == null) return;

        var vm = GetMainViewModel();
        _snippets = vm?.TabManager.ActiveTab != null
            ? _snippetService.GetByLanguage(vm.TabManager.ActiveTab.Language)
            : _snippetService.GetAll();

        // Apply category filter
        if (!string.IsNullOrEmpty(_selectedCategory))
            _snippets = _snippets.Where(s => s.Category == _selectedCategory).ToList();

        // Apply text filter
        if (!string.IsNullOrWhiteSpace(_filterText))
            _snippets = _snippets.Where(s =>
                s.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                s.Trigger.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                s.Content.Contains(_filterText, StringComparison.OrdinalIgnoreCase)).ToList();

        var listBox = this.FindControl<ListBox>("SnippetList");
        if (listBox == null) return;

        listBox.Items.Clear();
        foreach (var s in _snippets)
        {
            listBox.Items.Add(new ListBoxItem
            {
                Content = $"{s.Name} ({s.Trigger}) [{s.Category}]",
                Tag = s
            });
        }
    }

    private void OnCategoryFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: ComboBoxItem item })
        {
            _selectedCategory = item.Tag as string ?? "";
            RefreshList();
        }
    }

    private void OnSnippetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || lb.SelectedItem is not ListBoxItem { Tag: Snippet snippet }) return;

        var nameBox = this.FindControl<TextBox>("SnippetName");
        var triggerBox = this.FindControl<TextBox>("SnippetTrigger");
        var contentBox = this.FindControl<TextBox>("SnippetContent");
        var categoryBox = this.FindControl<TextBox>("SnippetCategory");

        if (nameBox != null) nameBox.Text = snippet.Name;
        if (triggerBox != null) triggerBox.Text = snippet.Trigger;
        if (contentBox != null) contentBox.Text = snippet.Content;
        if (categoryBox != null) categoryBox.Text = snippet.Category;
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (_snippetService == null) return;

        var nameBox = this.FindControl<TextBox>("SnippetName");
        var triggerBox = this.FindControl<TextBox>("SnippetTrigger");
        var contentBox = this.FindControl<TextBox>("SnippetContent");
        var categoryBox = this.FindControl<TextBox>("SnippetCategory");

        var name = nameBox?.Text?.Trim();
        var trigger = triggerBox?.Text?.Trim();
        var content = contentBox?.Text;
        var category = categoryBox?.Text?.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(trigger)) return;

        var vm = GetMainViewModel();
        var snippet = new Snippet
        {
            Name = name,
            Trigger = trigger,
            Content = content ?? string.Empty,
            Category = string.IsNullOrEmpty(category) ? "General" : category,
            Language = vm?.TabManager.ActiveTab?.Language ?? SupportedLanguage.PlainText
        };

        _snippetService.Add(snippet);
        _snippetService.Save();
        RefreshCategories();
        RefreshList();
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_snippetService == null) return;
        var listBox = this.FindControl<ListBox>("SnippetList");
        if (listBox?.SelectedItem is not ListBoxItem { Tag: Snippet snippet }) return;

        _snippetService.Delete(snippet.Name);
        _snippetService.Save();
        RefreshCategories();
        RefreshList();
    }

    private void OnInsertClick(object? sender, RoutedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("SnippetList");
        if (listBox?.SelectedItem is not ListBoxItem { Tag: Snippet snippet }) return;

        var vm = GetMainViewModel();
        if (vm == null) return;

        vm.AddToClipboardHistory(snippet.Content);
        if (vm.TabManager.ActiveTab != null)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                topLevel.Clipboard.SetTextAsync(snippet.Content).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.PasteCommand.Execute(null));
                });
            }
        }
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_snippetService == null || _dialogService == null) return;

        var filters = new[]
        {
            new Avalonia.Platform.Storage.FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
        };

        var file = await _dialogService.ShowSaveFileDialogAsync("Exporter les snippets", "snippets", "json", filters);
        if (file != null)
        {
            var json = _snippetService.ExportToJson();
            await File.WriteAllTextAsync(file.Path.LocalPath, json);
        }
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (_snippetService == null || _dialogService == null) return;

        var filters = new[]
        {
            new Avalonia.Platform.Storage.FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
        };

        var files = await _dialogService.ShowOpenFileDialogAsync("Importer des snippets", filters);
        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file.Path.LocalPath);
            _snippetService.ImportFromJson(json);
        }

        RefreshCategories();
        RefreshList();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var vm = GetMainViewModel();
        if (vm != null) vm.IsSnippetManagerVisible = false;
    }

    private ShellViewModel? GetMainViewModel()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel?.DataContext as ShellViewModel;
    }
}
