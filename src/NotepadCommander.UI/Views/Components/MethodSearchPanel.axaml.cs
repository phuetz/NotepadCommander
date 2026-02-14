using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.MethodExtractor;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class MethodSearchPanel : UserControl
{
    private readonly IMethodExtractorService _methodExtractorService;
    private string? _searchDirectory;
    private CancellationTokenSource? _cts;
    private List<MethodInfo> _foundMethods = new();

    public MethodSearchPanel()
    {
        InitializeComponent();
        _methodExtractorService = App.Services.GetRequiredService<IMethodExtractorService>();
    }

    public void SetSearchDirectory(string? directory)
    {
        _searchDirectory = directory;
    }

    public void FocusInput()
    {
        var box = this.FindControl<TextBox>("MethodNamesBox");
        box?.Focus();
    }

    private async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        await ExecuteSearch();
    }

    private async Task ExecuteSearch()
    {
        var namesBox = this.FindControl<TextBox>("MethodNamesBox");
        var statusText = this.FindControl<TextBlock>("StatusText");
        var resultsTree = this.FindControl<TreeView>("ResultsTree");
        var groupButton = this.FindControl<Button>("GroupButton");

        if (namesBox == null || statusText == null || resultsTree == null || groupButton == null)
            return;

        var input = namesBox.Text;
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(_searchDirectory))
        {
            statusText.Text = _searchDirectory == null
                ? "Ouvrez d'abord un dossier dans l'explorateur"
                : "Saisissez au moins un nom de methode";
            return;
        }

        // Parse method names from input (comma or newline separated)
        var methodNames = input
            .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct()
            .ToArray();

        if (methodNames.Length == 0)
        {
            statusText.Text = "Saisissez au moins un nom de methode";
            return;
        }

        // Cancel previous search
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _foundMethods.Clear();
        resultsTree.ItemsSource = null;
        groupButton.IsEnabled = false;
        statusText.Text = "Recherche en cours...";

        try
        {
            await foreach (var method in _methodExtractorService.ExtractMethods(
                _searchDirectory, methodNames, token))
            {
                _foundMethods.Add(method);
            }

            if (token.IsCancellationRequested) return;

            // Group results by file
            var groups = _foundMethods
                .GroupBy(m => m.FilePath)
                .Select(g => new MethodFileGroup
                {
                    FilePath = g.Key,
                    DisplayPath = GetRelativePath(g.Key),
                    Methods = g.Select(m => new MethodItem
                    {
                        Info = m,
                        DisplayText = m.Signature.Length > 80
                            ? m.Signature[..77] + "..."
                            : m.Signature
                    }).ToList()
                })
                .ToList();

            resultsTree.ItemsSource = groups;

            // Use a TreeDataTemplate via code
            resultsTree.ItemTemplate = new Avalonia.Controls.Templates.FuncTreeDataTemplate<object>(
                (item, _) =>
                {
                    if (item is MethodFileGroup group)
                        return new TextBlock
                        {
                            Text = $"{group.DisplayPath} ({group.Methods.Count})",
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            FontSize = 11
                        };
                    if (item is MethodItem mi)
                        return new TextBlock
                        {
                            Text = mi.DisplayText,
                            FontSize = 11,
                            Foreground = Avalonia.Media.Brushes.DarkSlateGray
                        };
                    return new TextBlock { Text = item?.ToString() ?? "" };
                },
                item =>
                {
                    if (item is MethodFileGroup group)
                        return (System.Collections.IEnumerable)group.Methods;
                    return System.Array.Empty<object>();
                });

            var methodCount = _foundMethods.Count;
            var fileCount = groups.Count;
            var notFound = methodNames.Except(
                _foundMethods.Select(m => m.MethodName), StringComparer.OrdinalIgnoreCase).ToList();

            var status = $"{methodCount} methode(s) trouvee(s) dans {fileCount} fichier(s)";
            if (notFound.Count > 0)
                status += $" | Non trouvee(s): {string.Join(", ", notFound)}";

            statusText.Text = status;
            groupButton.IsEnabled = methodCount > 0;
        }
        catch (OperationCanceledException)
        {
            statusText.Text = "Recherche annulee";
        }
        catch (Exception ex)
        {
            statusText.Text = $"Erreur: {ex.Message}";
        }
    }

    private async void OnGroupClick(object? sender, RoutedEventArgs e)
    {
        if (_foundMethods.Count == 0) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.DataContext is not MainWindowViewModel vm) return;

        var sb = new StringBuilder();

        foreach (var group in _foundMethods.GroupBy(m => m.FilePath))
        {
            foreach (var method in group)
            {
                var relativePath = GetRelativePath(method.FilePath);
                sb.AppendLine($"// === Fichier: {relativePath} (ligne {method.StartLine}) ===");
                sb.AppendLine();
                sb.AppendLine(method.FullBody);
                sb.AppendLine();
            }
        }

        // Create a new document with the grouped methods
        vm.NewDocumentCommand.Execute(null);
        if (vm.ActiveTab != null)
        {
            vm.ActiveTab.Content = sb.ToString();
        }
    }

    private async void OnResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView tree) return;

        MethodInfo? methodInfo = null;

        if (tree.SelectedItem is MethodItem item)
        {
            methodInfo = item.Info;
        }
        else if (tree.SelectedItem is MethodFileGroup group && group.Methods.Count > 0)
        {
            methodInfo = group.Methods[0].Info;
        }

        if (methodInfo == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.DataContext is not MainWindowViewModel vm) return;

        await vm.OpenFilePath(methodInfo.FilePath);

        if (vm.ActiveTab != null)
        {
            vm.ActiveTab.CursorLine = methodInfo.StartLine;
        }
    }

    private string GetRelativePath(string fullPath)
    {
        if (_searchDirectory == null) return fullPath;
        try
        {
            return Path.GetRelativePath(_searchDirectory, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}

internal class MethodFileGroup
{
    public string FilePath { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
    public List<MethodItem> Methods { get; set; } = new();
}

internal class MethodItem
{
    public MethodInfo Info { get; set; } = null!;
    public string DisplayText { get; set; } = string.Empty;
}
