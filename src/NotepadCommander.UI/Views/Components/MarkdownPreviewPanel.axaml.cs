using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services.Markdown;
using NotepadCommander.UI.Controls;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class MarkdownPreviewPanel : UserControl
{
    private IMarkdownService? _markdownService;
    private DispatcherTimer? _debounceTimer;
    private string? _pendingContent;
    private bool _servicesResolved;

    public MarkdownPreviewPanel()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            if (_pendingContent != null)
                RenderContent(_pendingContent);
        };
    }

    public void UpdatePreview(string markdownContent)
    {
        _pendingContent = markdownContent;
        _debounceTimer?.Stop();
        _debounceTimer?.Start();
    }

    private void RenderContent(string markdownContent)
    {
        if (_markdownService == null)
        {
            if (!_servicesResolved)
            {
                _servicesResolved = true;
                try { _markdownService = App.Services.GetService<IMarkdownService>(); } catch { return; }
            }
        }

        if (_markdownService == null) return;

        var html = _markdownService.ToHtml(markdownContent);
        var controls = MarkdownRenderer.RenderHtml(html);

        var panel = this.FindControl<StackPanel>("ContentPanel");
        if (panel == null) return;

        panel.Children.Clear();
        foreach (var control in controls)
            panel.Children.Add(control);
    }

    public void ClearPreview()
    {
        var panel = this.FindControl<StackPanel>("ContentPanel");
        panel?.Children.Clear();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is ShellViewModel vm)
        {
            vm.IsMarkdownPreviewVisible = false;
        }
    }
}
