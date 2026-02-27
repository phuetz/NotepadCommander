using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Git;
using NotepadCommander.UI.Controls;

namespace NotepadCommander.UI.Services;

public class DevFeaturesManager
{
    private TextEditor? _textEditor;
    private GitGutterMargin? _gitGutterMargin;
    private BracketHighlightRenderer? _bracketRenderer;
    private IndentationGuideRenderer? _indentGuideRenderer;
    private OccurrenceHighlightRenderer? _occurrenceRenderer;
    private FoldingManager? _foldingManager;
    private IGitService? _gitService;
    private DispatcherTimer? _gitDebounceTimer;
    private DispatcherTimer? _foldingDebounceTimer;

    public OccurrenceHighlightRenderer? OccurrenceRenderer => _occurrenceRenderer;
    public BracketHighlightRenderer? BracketRenderer => _bracketRenderer;

    public void Initialize(TextEditor textEditor, IGitService? gitService)
    {
        _textEditor = textEditor;
        _gitService = gitService;

        _gitGutterMargin = new GitGutterMargin();
        _textEditor.TextArea.LeftMargins.Insert(0, _gitGutterMargin);

        _bracketRenderer = new BracketHighlightRenderer();
        _textEditor.TextArea.TextView.BackgroundRenderers.Add(_bracketRenderer);

        _indentGuideRenderer = new IndentationGuideRenderer();
        _textEditor.TextArea.TextView.BackgroundRenderers.Add(_indentGuideRenderer);

        _occurrenceRenderer = new OccurrenceHighlightRenderer();
        _textEditor.TextArea.TextView.BackgroundRenderers.Add(_occurrenceRenderer);

        _foldingManager = FoldingManager.Install(_textEditor.TextArea);

        _gitDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _gitDebounceTimer.Tick += (_, _) =>
        {
            _gitDebounceTimer.Stop();
            RefreshGitGutter(null);
        };

        _foldingDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        _foldingDebounceTimer.Tick += (_, _) =>
        {
            _foldingDebounceTimer.Stop();
            RefreshFoldings(null, SupportedLanguage.PlainText);
        };
    }

    public void ScheduleRefresh()
    {
        _gitDebounceTimer?.Stop();
        _gitDebounceTimer?.Start();
        _foldingDebounceTimer?.Stop();
        _foldingDebounceTimer?.Start();
    }

    public void RefreshGitGutter(string? filePath)
    {
        if (_gitGutterMargin == null || _gitService == null || filePath == null) return;

        try
        {
            if (_gitService.IsInGitRepository(filePath))
            {
                var changes = _gitService.GetModifiedLines(filePath);
                _gitGutterMargin.UpdateChanges(changes);
            }
            else
            {
                _gitGutterMargin.UpdateChanges(Array.Empty<GitLineChange>());
            }
        }
        catch
        {
            _gitGutterMargin.UpdateChanges(Array.Empty<GitLineChange>());
        }
    }

    public void RefreshFoldings(string? filePath, SupportedLanguage language)
    {
        if (_foldingManager == null || _textEditor == null) return;

        try
        {
            CodeFoldingService.UpdateFoldings(_foldingManager, _textEditor.Document, language);
        }
        catch { }
    }

    public void UpdateBracketHighlight(int caretOffset)
    {
        if (_bracketRenderer == null || _textEditor == null) return;

        var (open, close) = BracketHighlightRenderer.FindMatchingBracket(
            _textEditor.Document, caretOffset);

        if (open >= 0 && close >= 0)
            _bracketRenderer.SetHighlight(open, close);
        else
            _bracketRenderer.ClearHighlight();

        _textEditor.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
    }

    public void UpdateOccurrenceHighlights(string? selectedText)
    {
        if (_occurrenceRenderer == null || _textEditor == null) return;

        if (string.IsNullOrWhiteSpace(selectedText) || selectedText.Contains('\n') || selectedText.Contains('\r'))
        {
            if (_occurrenceRenderer.Count > 0)
            {
                _occurrenceRenderer.ClearOccurrences();
                _textEditor.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
            }
            return;
        }

        var occurrences = OccurrenceHighlightRenderer.FindAllOccurrences(_textEditor.Document, selectedText);
        _occurrenceRenderer.SetOccurrences(occurrences);
        _textEditor.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
    }

    public void ReinstallFoldingManager()
    {
        if (_textEditor == null) return;
        try
        {
            if (_foldingManager != null)
                FoldingManager.Uninstall(_foldingManager);
        }
        catch { }
        try
        {
            _foldingManager = FoldingManager.Install(_textEditor.TextArea);
        }
        catch { }
    }

    /// <summary>
    /// Sets the file path context for debounce refresh callbacks.
    /// </summary>
    private string? _currentFilePath;
    private SupportedLanguage _currentLanguage;

    public void SetContext(string? filePath, SupportedLanguage language)
    {
        _currentFilePath = filePath;
        _currentLanguage = language;

        // Re-wire debounce callbacks with new context
        if (_gitDebounceTimer != null)
        {
            _gitDebounceTimer.Tick -= OnGitDebounce;
            _gitDebounceTimer.Tick += OnGitDebounce;
        }
        if (_foldingDebounceTimer != null)
        {
            _foldingDebounceTimer.Tick -= OnFoldingDebounce;
            _foldingDebounceTimer.Tick += OnFoldingDebounce;
        }
    }

    private void OnGitDebounce(object? sender, EventArgs e)
    {
        _gitDebounceTimer?.Stop();
        RefreshGitGutter(_currentFilePath);
    }

    private void OnFoldingDebounce(object? sender, EventArgs e)
    {
        _foldingDebounceTimer?.Stop();
        RefreshFoldings(_currentFilePath, _currentLanguage);
    }
}
