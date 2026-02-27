using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Snippets;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class SnippetManagerPanel : UserControl
{
    private ISnippetService? _snippetService;
    private List<Snippet> _snippets = new();
    private bool _servicesResolved;

    public SnippetManagerPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (!_servicesResolved)
        {
            _servicesResolved = true;
            try { _snippetService = App.Services.GetService<ISnippetService>(); } catch { }
        }
        RefreshList();
    }

    private void RefreshList()
    {
        if (_snippetService == null) return;

        var vm = GetMainViewModel();
        _snippets = vm?.TabManager.ActiveTab != null
            ? _snippetService.GetByLanguage(vm.TabManager.ActiveTab.Language)
            : _snippetService.GetAll();

        var listBox = this.FindControl<ListBox>("SnippetList");
        if (listBox == null) return;

        listBox.Items.Clear();
        foreach (var s in _snippets)
        {
            listBox.Items.Add(new ListBoxItem
            {
                Content = $"{s.Name} ({s.Trigger})",
                Tag = s
            });
        }
    }

    private void OnSnippetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || lb.SelectedItem is not ListBoxItem { Tag: Snippet snippet }) return;

        var nameBox = this.FindControl<TextBox>("SnippetName");
        var triggerBox = this.FindControl<TextBox>("SnippetTrigger");
        var contentBox = this.FindControl<TextBox>("SnippetContent");

        if (nameBox != null) nameBox.Text = snippet.Name;
        if (triggerBox != null) triggerBox.Text = snippet.Trigger;
        if (contentBox != null) contentBox.Text = snippet.Content;
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (_snippetService == null) return;

        var nameBox = this.FindControl<TextBox>("SnippetName");
        var triggerBox = this.FindControl<TextBox>("SnippetTrigger");
        var contentBox = this.FindControl<TextBox>("SnippetContent");

        var name = nameBox?.Text?.Trim();
        var trigger = triggerBox?.Text?.Trim();
        var content = contentBox?.Text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(trigger)) return;

        var vm = GetMainViewModel();
        var snippet = new Snippet
        {
            Name = name,
            Trigger = trigger,
            Content = content ?? string.Empty,
            Language = vm?.TabManager.ActiveTab?.Language ?? SupportedLanguage.PlainText
        };

        _snippetService.Add(snippet);
        _snippetService.Save();
        RefreshList();
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_snippetService == null) return;
        var listBox = this.FindControl<ListBox>("SnippetList");
        if (listBox?.SelectedItem is not ListBoxItem { Tag: Snippet snippet }) return;

        _snippetService.Delete(snippet.Name);
        _snippetService.Save();
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
