using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.Search;
using NotepadCommander.UI.Models;
using NotepadCommander.UI.Services;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class SearchPanel : UserControl
{
    private IMultiFileSearchService? _searchService;
    private ISearchReplaceService? _replaceService;
    private IDialogService? _dialogService;
    private CancellationTokenSource? _searchCts;
    private string? _searchRootDirectory;
    private bool _showReplace;
    private bool _servicesResolved;

    public SearchPanel()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_servicesResolved) return;
        _servicesResolved = true;
        try
        {
            _searchService = App.Services.GetRequiredService<IMultiFileSearchService>();
            _replaceService = App.Services.GetRequiredService<ISearchReplaceService>();
            _dialogService = App.Services.GetService<IDialogService>();
        }
        catch { }
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

    private void OnToggleReplaceClick(object? sender, RoutedEventArgs e)
    {
        _showReplace = !_showReplace;
        var section = this.FindControl<StackPanel>("ReplaceSection");
        var button = this.FindControl<Button>("ToggleReplaceButton");
        if (section != null) section.IsVisible = _showReplace;
        if (button != null) button.Content = _showReplace ? "\u25B2 Remplacer" : "\u25BC Remplacer";
    }

    private async void OnReplaceAllClick(object? sender, RoutedEventArgs e)
    {
        if (_searchService == null || _replaceService == null) return;

        var patternBox = this.FindControl<TextBox>("SearchPatternBox");
        var replaceBox = this.FindControl<TextBox>("ReplacePatternBox");
        var status = this.FindControl<TextBlock>("StatusText");

        var pattern = patternBox?.Text?.Trim();
        var replacement = replaceBox?.Text ?? string.Empty;

        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(_searchRootDirectory)) return;

        var caseSensitive = this.FindControl<CheckBox>("CaseSensitiveCheck")?.IsChecked == true;
        var wholeWord = this.FindControl<CheckBox>("WholeWordCheck")?.IsChecked == true;
        var useRegex = this.FindControl<CheckBox>("RegexCheck")?.IsChecked == true;

        if (status != null) status.Text = "Analyse en cours...";

        var window = TopLevel.GetTopLevel(this);
        var vm = window?.DataContext as ShellViewModel;

        try
        {
            var options = new MultiFileSearchOptions
            {
                CaseSensitive = caseSensitive,
                WholeWord = wholeWord,
                UseRegex = useRegex,
                MaxResults = 10000
            };

            var cts = new CancellationTokenSource();
            var fileResults = new List<FileSearchResult>();
            await foreach (var result in _searchService.SearchInDirectory(_searchRootDirectory, pattern, options, cts.Token))
            {
                fileResults.Add(result);
            }

            var filePaths = fileResults.Select(r => r.FilePath).Distinct().ToList();

            int totalOccurrences = 0;
            var fileOccurrences = new Dictionary<string, int>();

            foreach (var filePath in filePaths)
            {
                var content = await File.ReadAllTextAsync(filePath);
                var (_, count) = _replaceService.ReplaceAllWithCount(content, pattern, replacement, useRegex, caseSensitive, wholeWord);
                if (count > 0)
                {
                    totalOccurrences += count;
                    fileOccurrences[filePath] = count;
                }
            }

            if (fileOccurrences.Count == 0)
            {
                if (status != null) status.Text = "Aucune occurrence trouvee.";
                return;
            }

            var confirmed = _dialogService != null
                && await _dialogService.ShowConfirmDialogAsync(
                    "Remplacer dans les fichiers",
                    $"Remplacer '{pattern}' par '{replacement}'\ndans {fileOccurrences.Count} fichier(s) ({totalOccurrences} occurrence(s)) ?");

            if (!confirmed) return;

            if (status != null) status.Text = "Remplacement en cours...";

            int totalReplacements = 0;
            int filesChanged = 0;

            foreach (var (filePath, _) in fileOccurrences)
            {
                var openTab = vm?.TabManager.Tabs.FirstOrDefault(t =>
                    string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                if (openTab != null)
                {
                    var (newContent, count) = _replaceService.ReplaceAllWithCount(
                        openTab.Content, pattern, replacement, useRegex, caseSensitive, wholeWord);
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
                        content, pattern, replacement, useRegex, caseSensitive, wholeWord);
                    if (count > 0)
                    {
                        await File.WriteAllTextAsync(filePath, newContent);
                        totalReplacements += count;
                        filesChanged++;
                    }
                }
            }

            if (status != null)
                status.Text = $"{totalReplacements} remplacement(s) dans {filesChanged} fichier(s)";

            ExecuteSearch();
        }
        catch (Exception ex)
        {
            if (status != null) status.Text = $"Erreur: {ex.Message}";
        }
    }

    private async void ExecuteSearch()
    {
        if (_searchService == null) return;

        var patternBox = this.FindControl<TextBox>("SearchPatternBox");
        var pattern = patternBox?.Text?.Trim();
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(_searchRootDirectory)) return;

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

                if (results.Count == 0)
                    status.Text = "Aucun resultat trouve.";
                else
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
        if (window?.DataContext is ShellViewModel vm)
        {
            await vm.OpenFilePath(filePath);
            if (vm.TabManager.ActiveTab != null)
            {
                vm.TabManager.ActiveTab.CursorLine = line;
            }
        }
    }
}
