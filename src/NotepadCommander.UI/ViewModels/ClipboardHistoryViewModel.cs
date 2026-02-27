using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NotepadCommander.UI.ViewModels;

public partial class ClipboardHistoryViewModel : ViewModelBase
{
    private readonly List<string> _history = new();
    public IReadOnlyList<string> History => _history;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private string filter = string.Empty;

    [ObservableProperty]
    private int selectedIndex;

    [ObservableProperty]
    private string previewText = string.Empty;

    public ObservableCollection<string> FilteredItems { get; } = new();

    public event Action<string>? PasteRequested;

    public void AddToHistory(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _history.Remove(text);
        _history.Insert(0, text);
        if (_history.Count > 20)
            _history.RemoveAt(_history.Count - 1);
    }

    [RelayCommand]
    public void Show()
    {
        if (_history.Count == 0) return;
        Filter = string.Empty;
        RefreshFiltered();
        IsVisible = true;
    }

    [RelayCommand]
    public void Hide()
    {
        IsVisible = false;
    }

    public void MoveUp()
    {
        if (FilteredItems.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + FilteredItems.Count) % FilteredItems.Count;
        UpdatePreview();
    }

    public void MoveDown()
    {
        if (FilteredItems.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % FilteredItems.Count;
        UpdatePreview();
    }

    public void SelectCurrent()
    {
        if (SelectedIndex >= 0 && SelectedIndex < FilteredItems.Count)
        {
            var text = FilteredItems[SelectedIndex];
            IsVisible = false;
            PasteRequested?.Invoke(text);
        }
    }

    partial void OnFilterChanged(string value)
    {
        RefreshFiltered();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        UpdatePreview();
    }

    private void RefreshFiltered()
    {
        FilteredItems.Clear();
        var items = string.IsNullOrWhiteSpace(Filter)
            ? _history
            : _history.Where(h => h.Contains(Filter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var item in items)
            FilteredItems.Add(item);

        SelectedIndex = 0;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        PreviewText = SelectedIndex >= 0 && SelectedIndex < FilteredItems.Count
            ? FilteredItems[SelectedIndex]
            : string.Empty;
    }
}
