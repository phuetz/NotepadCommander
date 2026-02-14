using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Services;

namespace NotepadCommander.UI.ViewModels;

public partial class FindReplaceViewModel : ViewModelBase
{
    private readonly ISearchReplaceService _searchService;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string replaceText = string.Empty;

    [ObservableProperty]
    private bool useRegex;

    [ObservableProperty]
    private bool caseSensitive;

    [ObservableProperty]
    private bool wholeWord;

    [ObservableProperty]
    private bool showReplace;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private int matchCount;

    public event Action<SearchResult>? NavigateToResult;
    public event Action<string>? ReplaceAllText;
    public Func<string>? GetCurrentText;
    public Func<int>? GetCurrentOffset;

    public FindReplaceViewModel(ISearchReplaceService searchService)
    {
        _searchService = searchService;
    }

    [RelayCommand]
    private void FindNext()
    {
        var text = GetCurrentText?.Invoke() ?? string.Empty;
        var offset = (GetCurrentOffset?.Invoke() ?? 0) + 1;

        var result = _searchService.FindNext(text, SearchText, offset, UseRegex, CaseSensitive, WholeWord);
        if (result != null)
        {
            NavigateToResult?.Invoke(result);
            UpdateMatchCount(text);
        }
        else
        {
            StatusText = "Aucun resultat";
            MatchCount = 0;
        }
    }

    [RelayCommand]
    private void FindPrevious()
    {
        var text = GetCurrentText?.Invoke() ?? string.Empty;
        var offset = GetCurrentOffset?.Invoke() ?? 0;

        var result = _searchService.FindPrevious(text, SearchText, offset, UseRegex, CaseSensitive, WholeWord);
        if (result != null)
        {
            NavigateToResult?.Invoke(result);
            UpdateMatchCount(text);
        }
        else
        {
            StatusText = "Aucun resultat";
            MatchCount = 0;
        }
    }

    [RelayCommand]
    private void ReplaceNext()
    {
        FindNext();
    }

    [RelayCommand]
    private void ReplaceAllOccurrences()
    {
        var text = GetCurrentText?.Invoke() ?? string.Empty;
        var (result, count) = _searchService.ReplaceAllWithCount(text, SearchText, ReplaceText, UseRegex, CaseSensitive, WholeWord);

        if (count > 0)
        {
            ReplaceAllText?.Invoke(result);
            StatusText = $"{count} remplacement(s)";
            MatchCount = 0;
        }
        else
        {
            StatusText = "Aucun resultat";
        }
    }

    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
    }

    [RelayCommand]
    private void ToggleReplace()
    {
        ShowReplace = !ShowReplace;
    }

    public void Show(bool withReplace = false)
    {
        IsVisible = true;
        ShowReplace = withReplace;
    }

    private void UpdateMatchCount(string text)
    {
        var results = _searchService.FindAll(text, SearchText, UseRegex, CaseSensitive, WholeWord);
        MatchCount = results.Count;
        StatusText = $"{MatchCount} resultat(s)";
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            StatusText = string.Empty;
            MatchCount = 0;
            return;
        }

        var text = GetCurrentText?.Invoke() ?? string.Empty;
        UpdateMatchCount(text);
    }
}
