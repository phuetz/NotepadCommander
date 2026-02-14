using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class SearchPanel : UserControl
{
    private readonly IMultiFileSearchService _searchService;
    private CancellationTokenSource? _searchCts;
    private string? _searchRootDirectory;

    public SearchPanel()
    {
        InitializeComponent();
        _searchService = App.Services.GetRequiredService<IMultiFileSearchService>();
    }

    public void SetSearchDirectory(string? directory)
    {
        _searchRootDirectory = directory;
    }

    public void FocusSearchBox()
    {
        var box = this.FindControl<TextBox>("SearchPatternBox");
        box?.Focus();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ExecuteSearch();
            e.Handled = true;
        }
    }

    private void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        ExecuteSearch();
    }

    private async void ExecuteSearch()
    {
        var patternBox = this.FindControl<TextBox>("SearchPatternBox");
        var pattern = patternBox?.Text?.Trim();
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(_searchRootDirectory)) return;

        // Cancel previous search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        var caseSensitive = this.FindControl<CheckBox>("CaseSensitiveCheck")?.IsChecked == true;
        var wholeWord = this.FindControl<CheckBox>("WholeWordCheck")?.IsChecked == true;
        var useRegex = this.FindControl<CheckBox>("RegexCheck")?.IsChecked == true;

        var tree = this.FindControl<TreeView>("ResultsTree");
        var status = this.FindControl<TextBlock>("StatusText");
        if (tree == null || status == null) return;

        status.Text = "Recherche en cours...";
        tree.ItemsSource = null;

        var options = new MultiFileSearchOptions
        {
            CaseSensitive = caseSensitive,
            WholeWord = wholeWord,
            UseRegex = useRegex,
            MaxResults = 500
        };

        try
        {
            var results = new List<FileSearchResult>();
            await foreach (var result in _searchService.SearchInDirectory(_searchRootDirectory, pattern, options, ct))
            {
                results.Add(result);
            }

            if (ct.IsCancellationRequested) return;

            // Group by file
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

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                tree.Items.Clear();
                foreach (var group in grouped)
                {
                    var fileItem = new TreeViewItem
                    {
                        Header = BuildFileHeader(group),
                        Tag = group,
                        IsExpanded = true
                    };

                    foreach (var match in group.Matches)
                    {
                        fileItem.Items.Add(new TreeViewItem
                        {
                            Header = BuildMatchHeader(match),
                            Tag = match
                        });
                    }

                    tree.Items.Add(fileItem);
                }

                status.Text = $"{results.Count} resultat(s) dans {grouped.Count} fichier(s)";
            });
        }
        catch (OperationCanceledException)
        {
            // Search cancelled
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                status.Text = $"Erreur: {ex.Message}";
            });
        }
    }

    private string GetRelativePath(string fullPath)
    {
        if (_searchRootDirectory == null) return fullPath;
        try
        {
            return Path.GetRelativePath(_searchRootDirectory, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }

    private static StackPanel BuildFileHeader(SearchFileGroup group)
    {
        var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
        sp.Children.Add(new TextBlock
        {
            Text = group.FileName,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 11,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"({group.MatchCount})",
            FontSize = 10,
            Foreground = Avalonia.Media.Brushes.Gray,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = group.RelativePath,
            FontSize = 9,
            Foreground = Avalonia.Media.Brushes.Gray,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(4, 0, 0, 0)
        });
        return sp;
    }

    private static StackPanel BuildMatchHeader(SearchMatchItem match)
    {
        var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
        sp.Children.Add(new TextBlock
        {
            Text = $"L{match.Line}",
            FontSize = 10,
            Foreground = Avalonia.Media.Brushes.DarkCyan,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Width = 40
        });
        sp.Children.Add(new TextBlock
        {
            Text = match.LineText,
            FontSize = 11,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        return sp;
    }

    private async void OnResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView tree) return;
        var selected = tree.SelectedItem;

        string? filePath = null;
        int line = 1;

        // TreeViewItem.Tag holds the actual data object
        var tag = (selected as TreeViewItem)?.Tag ?? selected;

        if (tag is SearchMatchItem match)
        {
            filePath = match.FilePath;
            line = match.Line;
        }
        else if (tag is SearchFileGroup group)
        {
            filePath = group.FilePath;
        }

        if (filePath == null) return;

        var window = TopLevel.GetTopLevel(this);
        if (window?.DataContext is MainWindowViewModel vm)
        {
            await vm.OpenFilePath(filePath);
            if (vm.ActiveTab != null)
            {
                vm.ActiveTab.CursorLine = line;
            }
        }
    }
}

public class SearchFileGroup
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public int MatchCount { get; set; }
    public ObservableCollection<SearchMatchItem> Matches { get; set; } = new();
}

public class SearchMatchItem
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string LineText { get; set; } = string.Empty;
    public int MatchLength { get; set; }
}
