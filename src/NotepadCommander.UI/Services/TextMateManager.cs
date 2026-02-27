using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using NotepadCommander.Core.Models;
using TextMateSharp.Grammars;

namespace NotepadCommander.UI.Services;

public class TextMateManager : IDisposable
{
    private TextEditor? _textEditor;
    private TextMate.Installation? _installation;
    private RegistryOptions? _registryOptions;
    private bool? _currentThemeIsDark;

    public event Action<bool>? ThemeApplied;

    public void Initialize(TextEditor textEditor, bool isDark = false)
    {
        _textEditor = textEditor;
        InstallTheme(isDark);
    }

    public void SetTheme(bool isDark)
    {
        if (_textEditor == null || _currentThemeIsDark == isDark) return;
        InstallTheme(isDark);
    }

    public bool? CurrentThemeIsDark => _currentThemeIsDark;

    private void InstallTheme(bool isDark)
    {
        if (_textEditor == null) return;
        _currentThemeIsDark = isDark;

        // Dispose old installation
        if (_installation != null)
        {
            _installation.AppliedTheme -= OnAppliedTheme;
            _installation.Dispose();
            _installation = null;
        }

        var themeName = isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;
        _registryOptions = new RegistryOptions(themeName);
        _installation = _textEditor.InstallTextMate(_registryOptions);
        _installation.AppliedTheme += OnAppliedTheme;
    }

    public void InstallFallback()
    {
        if (_textEditor == null || _installation != null) return;
        _registryOptions = new RegistryOptions(ThemeName.LightPlus);
        _installation = _textEditor.InstallTextMate(_registryOptions);
        _installation.AppliedTheme += OnAppliedTheme;
    }

    public void ApplyGrammar(SupportedLanguage language)
    {
        if (_installation == null || _registryOptions == null) return;

        var scopeName = GetScopeName(language);
        if (scopeName != null)
            _installation.SetGrammar(scopeName);
    }

    private void OnAppliedTheme(object? sender, TextMate.Installation installation)
    {
        ThemeApplied?.Invoke(_currentThemeIsDark ?? false);
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

    public void Dispose()
    {
        if (_installation != null)
        {
            _installation.AppliedTheme -= OnAppliedTheme;
            _installation.Dispose();
            _installation = null;
        }
    }
}
