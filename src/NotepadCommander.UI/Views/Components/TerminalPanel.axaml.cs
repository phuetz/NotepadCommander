using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services.Terminal;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class TerminalPanel : UserControl
{
    private ITerminalService? _terminalService;
    private readonly List<string> _outputLines = new();
    private const int MaxOutputLines = 10000;
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private bool _servicesResolved;

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
            _outputLines.Add(text);
            while (_outputLines.Count > MaxOutputLines)
                _outputLines.RemoveAt(0);

            var outputText = this.FindControl<SelectableTextBlock>("OutputText");
            if (outputText != null)
                outputText.Text = string.Join("\n", _outputLines);

            var scrollViewer = this.FindControl<ScrollViewer>("OutputScrollViewer");
            scrollViewer?.ScrollToEnd();
        });
    }

    private void OnProcessExited()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _outputLines.Add("[Processus termine]");
            var outputText = this.FindControl<SelectableTextBlock>("OutputText");
            if (outputText != null)
                outputText.Text = string.Join("\n", _outputLines);
        });
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        var inputBox = this.FindControl<TextBox>("InputBox");
        if (inputBox == null) return;

        if (e.Key == Key.Enter)
        {
            var command = inputBox.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(command))
            {
                _commandHistory.Add(command);
                _historyIndex = _commandHistory.Count;
            }

            _outputLines.Add($"> {command}");
            var outputText = this.FindControl<SelectableTextBlock>("OutputText");
            if (outputText != null)
                outputText.Text = string.Join("\n", _outputLines);

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
    }

    private void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        _terminalService?.Stop();
        _outputLines.Clear();
        var outputText = this.FindControl<SelectableTextBlock>("OutputText");
        if (outputText != null)
            outputText.Text = string.Empty;

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
