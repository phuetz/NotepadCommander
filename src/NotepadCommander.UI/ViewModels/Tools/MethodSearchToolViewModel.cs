using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services.MethodExtractor;

namespace NotepadCommander.UI.ViewModels.Tools;

public class MethodFileGroup
{
    public string FilePath { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
    public List<MethodItem> Methods { get; set; } = new();
}

public class MethodItem
{
    public MethodInfo Info { get; set; } = null!;
    public string DisplayText { get; set; } = string.Empty;
}

public partial class MethodSearchToolViewModel : ToolViewModel
{
    private readonly IMethodExtractorService _methodExtractorService;
    private CancellationTokenSource? _cts;
    private List<MethodInfo> _foundMethods = new();

    [ObservableProperty]
    private string methodNamesInput = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string? searchDirectory;

    [ObservableProperty]
    private bool canGroup;

    public ObservableCollection<MethodFileGroup> ResultGroups { get; } = new();

    public override ToolLocation DefaultLocation => ToolLocation.Left;

    public event Action<string, int>? NavigateToFileRequested;
    public event Action<string>? GroupedResultRequested;

    public MethodSearchToolViewModel(IMethodExtractorService methodExtractorService)
    {
        _methodExtractorService = methodExtractorService;
        Title = "Methodes";
    }

    [RelayCommand]
    public async Task Search()
    {
        if (string.IsNullOrWhiteSpace(MethodNamesInput) || string.IsNullOrWhiteSpace(SearchDirectory))
        {
            StatusText = SearchDirectory == null
                ? "Ouvrez d'abord un dossier dans l'explorateur"
                : "Saisissez au moins un nom de methode";
            return;
        }

        var methodNames = MethodNamesInput
            .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct()
            .ToArray();

        if (methodNames.Length == 0)
        {
            StatusText = "Saisissez au moins un nom de methode";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _foundMethods.Clear();
        ResultGroups.Clear();
        CanGroup = false;
        StatusText = "Recherche en cours...";

        try
        {
            await foreach (var method in _methodExtractorService.ExtractMethods(
                SearchDirectory, methodNames, token))
            {
                _foundMethods.Add(method);
            }

            if (token.IsCancellationRequested) return;

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

            foreach (var group in groups)
                ResultGroups.Add(group);

            var methodCount = _foundMethods.Count;
            var fileCount = groups.Count;
            var notFound = methodNames.Except(
                _foundMethods.Select(m => m.MethodName), StringComparer.OrdinalIgnoreCase).ToList();

            var status = $"{methodCount} methode(s) trouvee(s) dans {fileCount} fichier(s)";
            if (notFound.Count > 0)
                status += $" | Non trouvee(s): {string.Join(", ", notFound)}";

            StatusText = status;
            CanGroup = methodCount > 0;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Recherche annulee";
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur: {ex.Message}";
        }
    }

    [RelayCommand]
    public void GroupResults()
    {
        if (_foundMethods.Count == 0) return;

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

        GroupedResultRequested?.Invoke(sb.ToString());
    }

    public void RequestNavigateToFile(string filePath, int line)
    {
        NavigateToFileRequested?.Invoke(filePath, line);
    }

    private string GetRelativePath(string fullPath)
    {
        if (SearchDirectory == null) return fullPath;
        try { return Path.GetRelativePath(SearchDirectory, fullPath); }
        catch { return fullPath; }
    }
}
