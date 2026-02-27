using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AvaloniaEdit.TextMate;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Git;
using NotepadCommander.UI.Services;
using TextMateSharp.Grammars;

namespace NotepadCommander.UI.Controls;

/// <summary>
/// Custom TextEditor subclass that encapsulates TextMate highlighting,
/// dev features (git gutter, bracket matching, folding, indentation guides),
/// and line operations. Inspired by AvalonStudio's CodeEditor pattern.
/// </summary>
public class NotepadEditor : TextEditor
{
    #region Styled Properties

    public static readonly StyledProperty<SupportedLanguage> LanguageProperty =
        AvaloniaProperty.Register<NotepadEditor, SupportedLanguage>(nameof(Language), SupportedLanguage.PlainText);

    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<NotepadEditor, bool>(nameof(IsDarkTheme), false);

    public static readonly StyledProperty<bool> ShowMinimapProperty =
        AvaloniaProperty.Register<NotepadEditor, bool>(nameof(ShowMinimap), false);

    public static readonly StyledProperty<bool> HighlightCurrentLineProperty =
        AvaloniaProperty.Register<NotepadEditor, bool>(nameof(HighlightCurrentLine), true);

    public static readonly StyledProperty<bool> ShowWhitespaceProperty =
        AvaloniaProperty.Register<NotepadEditor, bool>(nameof(ShowWhitespace), false);

    public SupportedLanguage Language
    {
        get => GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public bool IsDarkTheme
    {
        get => GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public bool ShowMinimap
    {
        get => GetValue(ShowMinimapProperty);
        set => SetValue(ShowMinimapProperty, value);
    }

    public bool HighlightCurrentLine
    {
        get => GetValue(HighlightCurrentLineProperty);
        set => SetValue(HighlightCurrentLineProperty, value);
    }

    public bool ShowWhitespace
    {
        get => GetValue(ShowWhitespaceProperty);
        set => SetValue(ShowWhitespaceProperty, value);
    }

    #endregion

    #region TextMate fields

    private TextMate.Installation? _tmInstallation;
    private RegistryOptions? _registryOptions;
    private bool? _currentTmThemeIsDark;

    #endregion

    #region Dev features fields

    private GitGutterMargin? _gitGutterMargin;
    private BracketHighlightRenderer? _bracketRenderer;
    private IndentationGuideRenderer? _indentGuideRenderer;
    private OccurrenceHighlightRenderer? _occurrenceRenderer;
    private FoldingManager? _foldingManager;

    private DispatcherTimer? _gitDebounceTimer;
    private DispatcherTimer? _foldingDebounceTimer;

    private string? _currentFilePath;
    private SupportedLanguage _currentLanguage;

    #endregion

    #region Public accessors for renderers

    public OccurrenceHighlightRenderer? OccurrenceRenderer => _occurrenceRenderer;
    public BracketHighlightRenderer? BracketRenderer => _bracketRenderer;

    #endregion

    private IGitService? _gitService;
    private bool _devFeaturesInitialized;
    private bool _textMateInitialized;

    public event Action<bool>? ThemeApplied;

    public NotepadEditor()
    {
        FontFamily = new FontFamily("Cascadia Code, Consolas, Menlo, monospace");
        FontSize = 14;
        ShowLineNumbers = true;
        WordWrap = false;
        IsReadOnly = false;
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
    }

    #region Initialization

    /// <summary>
    /// Inject the IGitService and initialize dev features.
    /// Call this once after the editor is attached to the visual tree.
    /// </summary>
    public void Initialize(IGitService? gitService)
    {
        _gitService = gitService;
        InitializeDevFeatures();
        InitializeTextMate();
    }

    private void InitializeTextMate()
    {
        if (_textMateInitialized) return;
        _textMateInitialized = true;
        InstallTextMateTheme(IsDarkTheme);
    }

    private void InitializeDevFeatures()
    {
        if (_devFeaturesInitialized) return;
        _devFeaturesInitialized = true;

        _gitGutterMargin = new GitGutterMargin();
        TextArea.LeftMargins.Insert(0, _gitGutterMargin);

        _bracketRenderer = new BracketHighlightRenderer();
        TextArea.TextView.BackgroundRenderers.Add(_bracketRenderer);

        _indentGuideRenderer = new IndentationGuideRenderer();
        TextArea.TextView.BackgroundRenderers.Add(_indentGuideRenderer);

        _occurrenceRenderer = new OccurrenceHighlightRenderer();
        TextArea.TextView.BackgroundRenderers.Add(_occurrenceRenderer);

        _foldingManager = FoldingManager.Install(TextArea);

        _gitDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _gitDebounceTimer.Tick += OnGitDebounce;

        _foldingDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        _foldingDebounceTimer.Tick += OnFoldingDebounce;
    }

    #endregion

    #region Property change handlers

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LanguageProperty)
        {
            var lang = change.GetNewValue<SupportedLanguage>();
            ApplyGrammar(lang);
            _currentLanguage = lang;
        }
        else if (change.Property == IsDarkThemeProperty)
        {
            var isDark = change.GetNewValue<bool>();
            if (_currentTmThemeIsDark != isDark)
                InstallTextMateTheme(isDark);
        }
        else if (change.Property == HighlightCurrentLineProperty)
        {
            Options.HighlightCurrentLine = change.GetNewValue<bool>();
        }
        else if (change.Property == ShowWhitespaceProperty)
        {
            var show = change.GetNewValue<bool>();
            Options.ShowEndOfLine = show;
            Options.ShowSpaces = show;
            Options.ShowTabs = show;
        }
    }

    #endregion

    #region TextMate management

    private void InstallTextMateTheme(bool isDark)
    {
        _currentTmThemeIsDark = isDark;

        if (_tmInstallation != null)
        {
            _tmInstallation.AppliedTheme -= OnTmThemeApplied;
            _tmInstallation.Dispose();
            _tmInstallation = null;
        }

        var themeName = isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;
        _registryOptions = new RegistryOptions(themeName);
        _tmInstallation = this.InstallTextMate(_registryOptions);
        _tmInstallation.AppliedTheme += OnTmThemeApplied;

        // Re-apply grammar if we have one
        if (_currentLanguage != SupportedLanguage.PlainText)
            ApplyGrammar(_currentLanguage);

        ApplyEditorColors(isDark);
    }

    public void ApplyGrammar(SupportedLanguage language)
    {
        if (_tmInstallation == null || _registryOptions == null) return;

        var scopeName = GetScopeName(language);
        if (scopeName != null)
            _tmInstallation.SetGrammar(scopeName);
    }

    private void OnTmThemeApplied(object? sender, TextMate.Installation installation)
    {
        ApplyEditorColors(_currentTmThemeIsDark ?? false);
        ThemeApplied?.Invoke(_currentTmThemeIsDark ?? false);
    }

    private void ApplyEditorColors(bool isDark)
    {
        Background = isDark
            ? new SolidColorBrush(Color.Parse("#1E1E1E"))
            : new SolidColorBrush(Colors.White);
        Foreground = isDark
            ? new SolidColorBrush(Color.Parse("#D4D4D4"))
            : new SolidColorBrush(Color.Parse("#333333"));
    }

    private string? GetScopeName(SupportedLanguage language) => language switch
    {
        SupportedLanguage.CSharp => _registryOptions?.GetScopeByLanguageId("csharp"),
        SupportedLanguage.JavaScript => _registryOptions?.GetScopeByLanguageId("javascript"),
        SupportedLanguage.TypeScript => _registryOptions?.GetScopeByLanguageId("typescript"),
        SupportedLanguage.Python => _registryOptions?.GetScopeByLanguageId("python"),
        SupportedLanguage.Java => _registryOptions?.GetScopeByLanguageId("java"),
        SupportedLanguage.Cpp => _registryOptions?.GetScopeByLanguageId("cpp"),
        SupportedLanguage.C => _registryOptions?.GetScopeByLanguageId("c"),
        SupportedLanguage.Html => _registryOptions?.GetScopeByLanguageId("html"),
        SupportedLanguage.Css => _registryOptions?.GetScopeByLanguageId("css"),
        SupportedLanguage.Xml => _registryOptions?.GetScopeByLanguageId("xml"),
        SupportedLanguage.Json => _registryOptions?.GetScopeByLanguageId("json"),
        SupportedLanguage.Yaml => _registryOptions?.GetScopeByLanguageId("yaml"),
        SupportedLanguage.Markdown => _registryOptions?.GetScopeByLanguageId("markdown"),
        SupportedLanguage.Sql => _registryOptions?.GetScopeByLanguageId("sql"),
        SupportedLanguage.PowerShell => _registryOptions?.GetScopeByLanguageId("powershell"),
        SupportedLanguage.Bash => _registryOptions?.GetScopeByLanguageId("shellscript"),
        SupportedLanguage.Ruby => _registryOptions?.GetScopeByLanguageId("ruby"),
        SupportedLanguage.Go => _registryOptions?.GetScopeByLanguageId("go"),
        SupportedLanguage.Rust => _registryOptions?.GetScopeByLanguageId("rust"),
        SupportedLanguage.Php => _registryOptions?.GetScopeByLanguageId("php"),
        _ => null
    };

    #endregion

    #region Dev features: Git gutter

    public void ScheduleRefresh()
    {
        _gitDebounceTimer?.Stop();
        _gitDebounceTimer?.Start();
        _foldingDebounceTimer?.Stop();
        _foldingDebounceTimer?.Start();
    }

    public void SetFileContext(string? filePath, SupportedLanguage language)
    {
        _currentFilePath = filePath;
        _currentLanguage = language;
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
        if (_foldingManager == null) return;

        try
        {
            CodeFoldingService.UpdateFoldings(_foldingManager, Document, language);
        }
        catch { }
    }

    public void ReinstallFoldingManager()
    {
        try
        {
            if (_foldingManager != null)
                FoldingManager.Uninstall(_foldingManager);
        }
        catch { }
        try
        {
            _foldingManager = FoldingManager.Install(TextArea);
        }
        catch { }
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

    #endregion

    #region Dev features: Bracket & occurrence highlighting

    public void UpdateBracketHighlight(int caretOffset)
    {
        if (_bracketRenderer == null) return;

        var (open, close) = BracketHighlightRenderer.FindMatchingBracket(Document, caretOffset);

        if (open >= 0 && close >= 0)
            _bracketRenderer.SetHighlight(open, close);
        else
            _bracketRenderer.ClearHighlight();

        TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
    }

    public void UpdateOccurrenceHighlights(string? selectedText)
    {
        if (_occurrenceRenderer == null) return;

        if (string.IsNullOrWhiteSpace(selectedText) || selectedText.Contains('\n') || selectedText.Contains('\r'))
        {
            if (_occurrenceRenderer.Count > 0)
            {
                _occurrenceRenderer.ClearOccurrences();
                TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
            }
            return;
        }

        var occurrences = OccurrenceHighlightRenderer.FindAllOccurrences(Document, selectedText);
        _occurrenceRenderer.SetOccurrences(occurrences);
        TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
    }

    #endregion

    #region Line operations

    public void DuplicateLine()
    {
        var doc = Document;
        var line = doc.GetLineByNumber(TextArea.Caret.Line);
        var lineText = doc.GetText(line.Offset, line.Length);
        doc.Insert(line.EndOffset, Environment.NewLine + lineText);
    }

    public void DeleteLine()
    {
        var doc = Document;
        var line = doc.GetLineByNumber(TextArea.Caret.Line);
        doc.Remove(line.Offset, line.TotalLength);
    }

    public void MoveLineUp()
    {
        var doc = Document;
        var caret = TextArea.Caret;
        var lineNum = caret.Line;
        if (lineNum <= 1) return;
        SwapLines(lineNum, lineNum - 1);
        caret.Line = lineNum - 1;
        ScrollTo(caret.Line, caret.Column);
    }

    public void MoveLineDown()
    {
        var doc = Document;
        var caret = TextArea.Caret;
        var lineNum = caret.Line;
        if (lineNum >= doc.LineCount) return;
        SwapLines(lineNum, lineNum + 1);
        caret.Line = lineNum + 1;
        ScrollTo(caret.Line, caret.Column);
    }

    private void SwapLines(int lineA, int lineB)
    {
        var doc = Document;
        var a = doc.GetLineByNumber(lineA);
        var b = doc.GetLineByNumber(lineB);
        var textA = doc.GetText(a.Offset, a.Length);
        var textB = doc.GetText(b.Offset, b.Length);

        doc.BeginUpdate();
        // Replace in order: higher line first to avoid offset shift
        if (lineA < lineB)
        {
            doc.Replace(b.Offset, b.Length, textA);
            var newA = doc.GetLineByNumber(lineA);
            doc.Replace(newA.Offset, newA.Length, textB);
        }
        else
        {
            doc.Replace(a.Offset, a.Length, textB);
            var newB = doc.GetLineByNumber(lineB);
            doc.Replace(newB.Offset, newB.Length, textA);
        }
        doc.EndUpdate();
    }

    public void GoToMatchingBracket()
    {
        var (open, close) = BracketHighlightRenderer.FindMatchingBracket(Document, CaretOffset);
        if (open < 0 || close < 0) return;

        var target = Math.Abs(CaretOffset - open) <= 1 ? close + 1 : open + 1;
        CaretOffset = target;
        var loc = Document.GetLocation(target);
        ScrollTo(loc.Line, loc.Column);
    }

    #endregion

    #region Cleanup

    public void Cleanup()
    {
        _gitDebounceTimer?.Stop();
        _foldingDebounceTimer?.Stop();

        if (_tmInstallation != null)
        {
            _tmInstallation.AppliedTheme -= OnTmThemeApplied;
            _tmInstallation.Dispose();
            _tmInstallation = null;
        }
    }

    #endregion
}
