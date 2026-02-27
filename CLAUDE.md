# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet build                                          # Build entire solution
dotnet run --project src/NotepadCommander.UI          # Run the app
dotnet test                                           # Run all 171 tests
dotnet test --filter "TextTransformServiceTests"      # Run a single test class
dotnet test --filter "FullyQualifiedName~MethodName"  # Run a single test method
dotnet run --project src/NotepadCommander.Cli -- format file.json json  # CLI tool
```

No linter or formatter is configured.

## Architecture

Four-project solution (.NET 8, C# 12, nullable enabled):

- **NotepadCommander.Core** — Models, interfaces, and stateless services (17 service directories: AutoComplete, AutoSave, Calculator, Compare, Encoding, Error, FileExplorer, FileWatcher, Git, Macro, Markdown, MethodExtractor, Search, Session, Snippets, Terminal, TextTransform)
- **NotepadCommander.UI** — Avalonia 11.2.1 desktop app with AvaloniaEdit 11.2.0 text editor, Fluent theme, ribbon interface (5 tabs)
- **NotepadCommander.Cli** — Console app for format/convert/diff/transform operations
- **NotepadCommander.Tests** — xUnit 2.6.6 unit tests mirroring service names

### MVVM + DI Pattern

- ViewModels extend `ViewModelBase : ObservableObject` (CommunityToolkit.Mvvm 8.2.2)
- Properties use `[ObservableProperty]`, commands use `[RelayCommand]`
- Services registered as **Singletons**, ViewModels as **Transients** in `App.axaml.cs`
- Global access via `App.Services` static property
- **Event delegation**: `MainWindowViewModel` emits ~17 events (e.g., `UndoRequested`, `DuplicateLineRequested`, `MoveLineRequested`), `EditorControl` code-behind subscribes in `ConnectToMainViewModel()` and bridges them to AvaloniaEdit APIs

### Key Dependencies

| Package | Purpose |
|---------|---------|
| AvaloniaEdit + TextMate | Code editor with 20+ language syntax highlighting |
| DiffPlex | File comparison/diff |
| Markdig | Markdown rendering |
| NCalcSync | Math expression calculator |

### Editor Architecture

`EditorControl.axaml.cs` is the most complex file — it bridges ViewModel events to AvaloniaEdit:

- **Dual editor**: `PrimaryEditor` (bound to `ActiveTab`) and `SecondaryEditor` (bound to `SecondaryTab`, toggled by `IsSplitViewActive`) share the same Grid cell in `MainWindow.axaml`
- **Dev features** (custom renderers on TextArea): GitGutterMargin, BracketHighlightRenderer, IndentationGuideRenderer, OccurrenceHighlightRenderer, FoldingManager — all initialized in `InitializeDevFeatures()`
- **Debouncing**: Git gutter refresh 500ms, code folding refresh 1000ms

## Critical AvaloniaEdit Gotchas

1. **App.axaml MUST include** `<StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />` — without it, TextArea template never loads (no input, no rendering)
2. **Focus**: `TextEditor.Focusable` is false by default — always call `TextArea.Focus()`, not `TextEditor.Focus()`
3. **TextMate grammar-first pattern**: When loading content, set grammar BEFORE setting document text. TextMate needs an active grammar to properly tokenize. Follow the order: `ApplySyntaxHighlighting()` → `_textEditor.Text = content`. This matches the official AvaloniaEdit demo.
4. **TextMate lifecycle**: Dispose old `TextMate.Installation` before calling `InstallTextMate()` again. Subscribe to `AppliedTheme` event for color application. Only reinstall when theme actually changes.
5. **FoldingManager + Document replacement**: When replacing `_textEditor.Document`, the FoldingManager must be uninstalled from the old document and reinstalled on the new one (`ReinstallFoldingManager()`).
6. **Compiled bindings**: `x:DataType` compiled bindings interfere with AvaloniaEdit — use `x:CompileBindings="False"` on UserControls wrapping it
7. **KeyBindings**: `{Binding $self}` passes the KeyBinding object, not the Window — use `GetWindow()` with fallback to `ApplicationLifetime.MainWindow`
8. **WinExe output**: `Console.WriteLine` is invisible — use file-based logging to `%TEMP%` for debugging

## Conventions

- **French UI**: All user-facing strings and many code comments are in French
- **Namespace = folder path**: e.g., `NotepadCommander.UI.Views.Components`
- **All services have interfaces** (IServiceName pattern)
- `InternalsVisibleTo` exposes Core internals to Tests project
