using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Encoding;
using NotepadCommander.Core.Services.AutoSave;
using NotepadCommander.Core.Services.Error;
using NotepadCommander.Core.Services.FileExplorer;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.Core.Services.Session;
using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.Macro;
using NotepadCommander.Core.Services.Snippets;
using NotepadCommander.Core.Services.Markdown;
using NotepadCommander.Core.Services.Calculator;
using NotepadCommander.Core.Services.AutoComplete;
using NotepadCommander.Core.Services.Git;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.UI.ViewModels;
using NotepadCommander.UI.Views;

namespace NotepadCommander.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Core Services
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<ISearchReplaceService, SearchReplaceService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ITextTransformService, TextTransformService>();
        services.AddSingleton<ICommentService, CommentService>();
        services.AddSingleton<IEncodingService, EncodingService>();
        services.AddSingleton<ILineEndingService, LineEndingService>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        services.AddSingleton<IFileWatcherService, FileWatcherService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IAutoSaveService, AutoSaveService>();
        services.AddSingleton<IFileExplorerService, FileExplorerService>();
        services.AddSingleton<ICompareService, CompareService>();
        services.AddSingleton<IMacroService, MacroService>();
        services.AddSingleton<ISnippetService, SnippetService>();
        services.AddSingleton<IMarkdownService, MarkdownService>();
        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<IMultiFileSearchService, MultiFileSearchService>();

        // ViewModels (MainWindowViewModel takes all services via DI constructor)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ToolbarViewModel>();
        services.AddTransient<FindReplaceViewModel>();
    }
}
