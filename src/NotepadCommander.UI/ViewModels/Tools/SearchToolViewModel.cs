using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.UI.Models;
using NotepadCommander.UI.Services;

namespace NotepadCommander.UI.ViewModels.Tools;

public partial class SearchToolViewModel : ToolViewModel
{
    private readonly IMultiFileSearchService _searchService;
    private readonly ISearchReplaceService _replaceService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    private string searchPattern = string.Empty;

    [ObservableProperty]
    private string replacePattern = string.Empty;

    [ObservableProperty]
    private bool caseSensitive;

    [ObservableProperty]
    private bool wholeWord;

    [ObservableProperty]
    private bool useRegex;

    [ObservableProperty]
    private bool showReplace;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string? searchRootDirectory;

    public ObservableCollection<SearchFileGroup> ResultGroups { get; } = new();

    public override ToolLocation DefaultLocation => ToolLocation.Left;

    public event Action<string, int>? NavigateToFileRequested;

    public SearchToolViewModel(
        IMultiFileSearchService searchService,
        ISearchReplaceService replaceService,
        IDialogService dialogService)
    {
        _searchService = searchService;
        _replaceService = replaceService;
        _dialogService = dialogService;
        Title = "Recherche";
    }

    [RelayCommand]
    public void ToggleReplace()
    {
        ShowReplace = !ShowReplace;
    }

    [RelayCommand]
    public async Task Search()
    {
        if (string.IsNullOrEmpty(SearchPattern) || string.IsNullOrEmpty(SearchRootDirectory)) return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        StatusText = "Recherche en cours...";
        ResultGroups.Clear();

        var options = new MultiFileSearchOptions
        {
            CaseSensitive = CaseSensitive,
            WholeWord = WholeWord,
            UseRegex = UseRegex,
            MaxResults = 2000
        };

        try
        {
            var results = new List<FileSearchResult>();
            await foreach (var result in _searchService.SearchInDirectory(SearchRootDirectory, SearchPattern, options, ct))
            {
                results.Add(result);
            }

            if (ct.IsCancellationRequested) return;

            var grouped = results
                .GroupBy(r => r.FilePath)
                .Select(g => new SearchFileGroup
                {
                    FilePath = g.Key,
                    FileName = Path.GetFileName(g.Key),
                    RelativePath = GetRelativePath(g.Key),
                    MatchCount = g.Count(),
                    Matches = new ObservableCollection<SearchMatchItem>(
                        g.Select(r => new SearchMatchItem
                        {
                            FilePath = r.FilePath,
                            Line = r.Line,
                            Column = r.Column,
                            LineText = r.LineText.Trim(),
                            MatchLength = r.MatchLength
                        }))
                })
                .ToList();

            foreach (var group in grouped)
                ResultGroups.Add(group);

            StatusText = results.Count == 0
                ? "Aucun resultat trouve."
                : $"{results.Count} resultat(s) dans {grouped.Count} fichier(s)";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = $"Erreur: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ReplaceAll(TabManagerViewModel? tabManager)
    {
        if (string.IsNullOrEmpty(SearchPattern) || string.IsNullOrEmpty(SearchRootDirectory)) return;

        StatusText = "Analyse en cours...";

        try
        {
            var options = new MultiFileSearchOptions
            {
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWord,
                UseRegex = UseRegex,
                MaxResults = 10000
            };

            var cts = new CancellationTokenSource();
            var fileResults = new List<FileSearchResult>();
            await foreach (var result in _searchService.SearchInDirectory(SearchRootDirectory, SearchPattern, options, cts.Token))
                fileResults.Add(result);

            var filePaths = fileResults.Select(r => r.FilePath).Distinct().ToList();

            int totalOccurrences = 0;
            var fileOccurrences = new Dictionary<string, int>();

            foreach (var filePath in filePaths)
            {
                var content = await File.ReadAllTextAsync(filePath);
                var (_, count) = _replaceService.ReplaceAllWithCount(content, SearchPattern, ReplacePattern, UseRegex, CaseSensitive, WholeWord);
                if (count > 0)
                {
                    totalOccurrences += count;
                    fileOccurrences[filePath] = count;
                }
            }

            if (fileOccurrences.Count == 0)
            {
                StatusText = "Aucune occurrence trouvee.";
                return;
            }

            var confirmed = await _dialogService.ShowConfirmDialogAsync(
                "Remplacer dans les fichiers",
                $"Remplacer '{SearchPattern}' par '{ReplacePattern}'\ndans {fileOccurrences.Count} fichier(s) ({totalOccurrences} occurrence(s)) ?");

            if (!confirmed) return;

            StatusText = "Remplacement en cours...";

            int totalReplacements = 0;
            int filesChanged = 0;

            foreach (var (filePath, _) in fileOccurrences)
            {
                var openTab = tabManager?.Tabs.FirstOrDefault(t =>
                    string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                if (openTab != null)
                {
                    var (newContent, count) = _replaceService.ReplaceAllWithCount(
                        openTab.Content, SearchPattern, ReplacePattern, UseRegex, CaseSensitive, WholeWord);
                    if (count > 0)
                    {
                        openTab.Content = newContent;
                        totalReplacements += count;
                        filesChanged++;
                    }
                }
                else
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var (newContent, count) = _replaceService.ReplaceAllWithCount(
                        content, SearchPattern, ReplacePattern, UseRegex, CaseSensitive, WholeWord);
                    if (count > 0)
                    {
                        await File.WriteAllTextAsync(filePath, newContent);
                        totalReplacements += count;
                        filesChanged++;
                    }
                }
            }

            StatusText = $"{totalReplacements} remplacement(s) dans {filesChanged} fichier(s)";
            await Search();
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur: {ex.Message}";
        }
    }

    public void RequestNavigateToFile(string filePath, int line)
    {
        NavigateToFileRequested?.Invoke(filePath, line);
    }

    private string GetRelativePath(string fullPath)
    {
        if (SearchRootDirectory == null) return fullPath;
        try { return Path.GetRelativePath(SearchRootDirectory, fullPath); }
        catch { return fullPath; }
    }
}
