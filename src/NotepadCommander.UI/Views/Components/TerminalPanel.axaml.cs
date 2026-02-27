using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services.Terminal;
using NotepadCommander.UI.Controls;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class TerminalPanel : UserControl
{
    private ITerminalService? _terminalService;
    private readonly List<string> _rawLines = new();
    private const int MaxOutputLines = 10000;
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private bool _servicesResolved;

    // Tab completion
    private static readonly string[] CommonCommands =
    {
        "cd", "cls", "clear", "dir", "ls", "git", "dotnet", "npm", "npx",
        "node", "python", "pip", "mkdir", "rmdir", "copy", "move", "del",
        "echo", "type", "cat", "grep", "find", "curl", "wget"
    };

    public TerminalPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        EnsureTerminalStarted();
    }

    private void EnsureTerminalStarted()
    {
        if (_terminalService != null) return;

        if (!_servicesResolved)
        {
            _servicesResolved = true;
            try { _terminalService = App.Services.GetService<ITerminalService>(); } catch { return; }
        }

        if (_terminalService == null) return;

        _terminalService.OutputReceived += OnOutputReceived;
        _terminalService.ProcessExited += OnProcessExited;

        var workingDir = GetWorkingDirectory();
        _terminalService.Start(workingDir);
    }

    private string GetWorkingDirectory()
    {
        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is ShellViewModel vm && vm.TabManager.ActiveTab?.FilePath != null)
        {
            var dir = System.IO.Path.GetDirectoryName(vm.TabManager.ActiveTab.FilePath);
            if (dir != null && System.IO.Directory.Exists(dir))
                return dir;
        }
        return Environment.CurrentDirectory;
    }

    private void OnOutputReceived(string text)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _rawLines.Add(text);
            while (_rawLines.Count > MaxOutputLines)
                _rawLines.RemoveAt(0);

            RefreshOutput();
        });
    }

    private void OnProcessExited()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _rawLines.Add("[Processus termine]");
            RefreshOutput();
        });
    }

    private void RefreshOutput()
    {
        var repeater = this.FindControl<ItemsControl>("OutputRepeater");
        if (repeater == null) return;

        // Build TextBlocks with ANSI color support
        var controls = new List<Control>();
        foreach (var line in _rawLines)
        {
            var spans = AnsiTextParser.Parse(line);
            var tb = new TextBlock
            {
                FontFamily = new FontFamily("Cascadia Code, Consolas, Menlo, monospace"),
                FontSize = 13,
                TextWrapping = TextWrapping.NoWrap
            };

            if (spans.Count == 1 && spans[0].Foreground == null && !spans[0].IsBold)
            {
                tb.Text = spans[0].Text;
            }
            else
            {
                foreach (var span in spans)
                {
                    var run = new Avalonia.Controls.Documents.Run(span.Text);
                    if (span.Foreground != null)
                        run.Foreground = span.Foreground;
                    if (span.IsBold)
                        run.FontWeight = FontWeight.Bold;
                    tb.Inlines!.Add(run);
                }
            }

            controls.Add(tb);
        }

        repeater.ItemsSource = controls;

        var scrollViewer = this.FindControl<ScrollViewer>("OutputScrollViewer");
        scrollViewer?.ScrollToEnd();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        var inputBox = this.FindControl<TextBox>("InputBox");
        if (inputBox == null) return;

        // Hide suggestions on any key except Tab
        if (e.Key != Key.Tab)
            HideSuggestions();

        if (e.Key == Key.Enter)
        {
            var command = inputBox.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(command))
            {
                _commandHistory.Add(command);
                _historyIndex = _commandHistory.Count;
            }

            _rawLines.Add($"> {command}");
            RefreshOutput();

            _terminalService?.SendInput(command);
            inputBox.Text = string.Empty;
            e.Handled = true;
        }
        else if (e.Key == Key.Up && _commandHistory.Count > 0)
        {
            _historyIndex = Math.Max(0, _historyIndex - 1);
            inputBox.Text = _commandHistory[_historyIndex];
            inputBox.CaretIndex = inputBox.Text?.Length ?? 0;
            e.Handled = true;
        }
        else if (e.Key == Key.Down && _commandHistory.Count > 0)
        {
            _historyIndex = Math.Min(_commandHistory.Count, _historyIndex + 1);
            inputBox.Text = _historyIndex < _commandHistory.Count
                ? _commandHistory[_historyIndex]
                : string.Empty;
            inputBox.CaretIndex = inputBox.Text?.Length ?? 0;
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            HandleTabCompletion(inputBox);
            e.Handled = true;
        }
    }

    private void HandleTabCompletion(TextBox inputBox)
    {
        var text = inputBox.Text?.TrimStart() ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        // Split into parts to complete the last token
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lastToken = parts[^1];
        var prefix = parts.Length > 1 ? string.Join(" ", parts[..^1]) + " " : "";

        // Try command completion for single-word input
        var suggestions = new List<string>();

        if (parts.Length == 1)
        {
            suggestions.AddRange(CommonCommands.Where(c =>
                c.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase)));
        }

        // Try path completion
        try
        {
            var dir = System.IO.Path.GetDirectoryName(lastToken);
            var search = System.IO.Path.GetFileName(lastToken);
            if (string.IsNullOrEmpty(dir)) dir = ".";

            if (System.IO.Directory.Exists(dir))
            {
                var entries = System.IO.Directory.GetFileSystemEntries(dir, search + "*")
                    .Select(e => System.IO.Path.GetFileName(e))
                    .Take(10)
                    .ToList();
                suggestions.AddRange(entries);
            }
        }
        catch { /* path completion failed, ignore */ }

        if (suggestions.Count == 1)
        {
            inputBox.Text = prefix + suggestions[0];
            inputBox.CaretIndex = inputBox.Text.Length;
            HideSuggestions();
        }
        else if (suggestions.Count > 1)
        {
            ShowSuggestions(suggestions, inputBox, prefix);
        }
    }

    private void ShowSuggestions(List<string> suggestions, TextBox inputBox, string prefix)
    {
        var panel = this.FindControl<Border>("SuggestionsPanel");
        var repeater = this.FindControl<ItemsControl>("SuggestionsRepeater");
        if (panel == null || repeater == null) return;

        var buttons = suggestions.Select(s =>
        {
            var btn = new Button
            {
                Content = s,
                Classes = { "btn-toolbar" },
                FontFamily = new FontFamily("Cascadia Code, Consolas, Menlo, monospace"),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Avalonia.Thickness(8, 2)
            };
            btn.Click += (_, _) =>
            {
                inputBox.Text = prefix + s;
                inputBox.CaretIndex = inputBox.Text.Length;
                HideSuggestions();
                inputBox.Focus();
            };
            return (Control)btn;
        }).ToList();

        repeater.ItemsSource = buttons;
        panel.IsVisible = true;
    }

    private void HideSuggestions()
    {
        var panel = this.FindControl<Border>("SuggestionsPanel");
        if (panel != null) panel.IsVisible = false;
    }

    private void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        _terminalService?.Stop();
        _rawLines.Clear();
        RefreshOutput();

        var workingDir = GetWorkingDirectory();
        _terminalService?.Start(workingDir);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is ShellViewModel vm)
        {
            vm.IsTerminalVisible = false;
        }
    }
}
