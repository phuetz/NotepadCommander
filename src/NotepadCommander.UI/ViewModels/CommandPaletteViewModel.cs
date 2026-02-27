using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.UI.Models;

namespace NotepadCommander.UI.ViewModels;

public partial class CommandPaletteViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private string query = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommandPaletteItem> items = new();

    [ObservableProperty]
    private int selectedIndex;

    private List<CommandPaletteItem> _allCommands = new();

    public void SetCommands(List<CommandPaletteItem> commands)
    {
        _allCommands = commands;
    }

    [RelayCommand]
    public void Show()
    {
        Query = string.Empty;
        IsVisible = true;
        FilterItems(string.Empty);
    }

    [RelayCommand]
    public void Hide()
    {
        IsVisible = false;
    }

    [RelayCommand]
    public void ExecuteItem(CommandPaletteItem? item)
    {
        if (item == null) return;
        IsVisible = false;
        item.Execute();
    }

    public void MoveUp()
    {
        if (Items.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + Items.Count) % Items.Count;
    }

    public void MoveDown()
    {
        if (Items.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Items.Count;
    }

    public void Confirm()
    {
        if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
            ExecuteItem(Items[SelectedIndex]);
    }

    partial void OnQueryChanged(string value)
    {
        FilterItems(value);
    }

    private void FilterItems(string query)
    {
        Items.Clear();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allCommands
            : _allCommands
                .Where(c => FuzzyMatch(c.Label, query) || c.Shortcut.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => FuzzyScore(c.Label, query))
                .ToList();

        foreach (var item in filtered)
            Items.Add(item);

        SelectedIndex = 0;
    }

    /// <summary>
    /// Fuzzy match: all query characters must appear in-order in the target.
    /// </summary>
    private static bool FuzzyMatch(string target, string query)
    {
        int qi = 0;
        for (int ti = 0; ti < target.Length && qi < query.Length; ti++)
        {
            if (char.ToLowerInvariant(target[ti]) == char.ToLowerInvariant(query[qi]))
                qi++;
        }
        return qi == query.Length;
    }

    /// <summary>
    /// Simple fuzzy score: consecutive matches and prefix matches score higher.
    /// </summary>
    private static int FuzzyScore(string target, string query)
    {
        int score = 0;
        int qi = 0;
        bool prevMatched = false;
        for (int ti = 0; ti < target.Length && qi < query.Length; ti++)
        {
            if (char.ToLowerInvariant(target[ti]) == char.ToLowerInvariant(query[qi]))
            {
                score += prevMatched ? 3 : 1; // consecutive bonus
                if (ti == qi) score += 2; // prefix bonus
                prevMatched = true;
                qi++;
            }
            else
            {
                prevMatched = false;
            }
        }
        return score;
    }
}
