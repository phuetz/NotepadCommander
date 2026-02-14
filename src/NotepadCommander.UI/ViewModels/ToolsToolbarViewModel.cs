using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.Macro;
using NotepadCommander.Core.Services.Snippets;
using NotepadCommander.Core.Services.Markdown;
using NotepadCommander.Core.Services.Calculator;
using NotepadCommander.Core.Models;

namespace NotepadCommander.UI.ViewModels;

public partial class ToolsToolbarViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVm;
    private readonly ICompareService _compareService;
    private readonly IMacroService _macroService;
    private readonly ISnippetService _snippetService;
    private readonly IMarkdownService _markdownService;
    private readonly ICalculatorService _calculatorService;

    [ObservableProperty]
    private bool isTerminalVisible;

    [ObservableProperty]
    private bool isMarkdownPreviewVisible;

    public ToolsToolbarViewModel(MainWindowViewModel mainVm)
    {
        _mainVm = mainVm;
        _compareService = App.Services.GetRequiredService<ICompareService>();
        _macroService = App.Services.GetRequiredService<IMacroService>();
        _snippetService = App.Services.GetRequiredService<ISnippetService>();
        _markdownService = App.Services.GetRequiredService<IMarkdownService>();
        _calculatorService = App.Services.GetRequiredService<ICalculatorService>();
    }

    public string RecordingIcon => _macroService.IsRecording ? "⏹" : "⏺";
    public string RecordingLabel => _macroService.IsRecording ? "Arrêter" : "Enregistrer";

    [RelayCommand]
    private void CompareFiles()
    {
        // Comparison requires two files - for now uses first two open tabs
        var tabs = _mainVm.Tabs;
        if (tabs.Count < 2) return;

        var oldText = tabs[0].Content ?? string.Empty;
        var newText = tabs[1].Content ?? string.Empty;
        var result = _compareService.Compare(oldText, newText);

        // Store the result for the diff view
        _mainVm.LastDiffResult = result;
        _mainVm.IsDiffViewVisible = true;
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        if (_macroService.IsRecording)
            _macroService.StopRecording();
        else
            _macroService.StartRecording();

        OnPropertyChanged(nameof(RecordingIcon));
        OnPropertyChanged(nameof(RecordingLabel));
    }

    [RelayCommand]
    private void PlayMacro()
    {
        var macro = _macroService.GetLastRecording();
        if (macro == null || _mainVm.ActiveTab == null) return;

        foreach (var step in macro.Steps)
        {
            if (step.Action == MacroAction.InsertText && step.Value != null)
            {
                var content = _mainVm.ActiveTab.Content ?? string.Empty;
                var pos = Math.Min(step.Position, content.Length);
                _mainVm.ActiveTab.Content = content.Insert(pos, step.Value);
            }
        }
    }

    [RelayCommand]
    private void ManageSnippets()
    {
        _mainVm.IsSnippetManagerVisible = !_mainVm.IsSnippetManagerVisible;
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
        _mainVm.IsTerminalVisible = IsTerminalVisible;
    }

    [RelayCommand]
    private void ToggleMarkdownPreview()
    {
        IsMarkdownPreviewVisible = !IsMarkdownPreviewVisible;
        _mainVm.IsMarkdownPreviewVisible = IsMarkdownPreviewVisible;
    }

    [RelayCommand]
    private void CalculateSelection()
    {
        var tab = _mainVm.ActiveTab;
        if (tab == null) return;

        var selectedText = _mainVm.SelectedText;
        if (string.IsNullOrWhiteSpace(selectedText)) return;

        var result = _calculatorService.Evaluate(selectedText);
        if (result.Success)
        {
            _mainVm.CalculationResult = $"{selectedText} = {result.Result}";
        }
        else
        {
            _mainVm.CalculationResult = $"Erreur : {result.Error}";
        }
    }
}
