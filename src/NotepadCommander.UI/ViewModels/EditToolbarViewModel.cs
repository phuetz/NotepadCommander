using CommunityToolkit.Mvvm.Input;
using NotepadCommander.Core.Services.TextTransform;

namespace NotepadCommander.UI.ViewModels;

public partial class EditToolbarViewModel : ViewModelBase
{
    private readonly ITextTransformService _transformService;
    private readonly ICommentService _commentService;
    private readonly MainWindowViewModel _mainViewModel;

    public EditToolbarViewModel(
        ITextTransformService transformService,
        ICommentService commentService,
        MainWindowViewModel mainViewModel)
    {
        _transformService = transformService;
        _commentService = commentService;
        _mainViewModel = mainViewModel;
    }

    private void ApplyTransform(Func<string, string> transform)
    {
        var tab = _mainViewModel.ActiveTab;
        if (tab == null) return;
        tab.Content = transform(tab.Content);
    }

    [RelayCommand]
    private void SortLinesAsc() => ApplyTransform(t => _transformService.SortLines(t, true));

    [RelayCommand]
    private void SortLinesDesc() => ApplyTransform(t => _transformService.SortLines(t, false));

    [RelayCommand]
    private void RemoveDuplicates() => ApplyTransform(_transformService.RemoveDuplicateLines);

    [RelayCommand]
    private void ToUpperCase() => ApplyTransform(_transformService.ToUpperCase);

    [RelayCommand]
    private void ToLowerCase() => ApplyTransform(_transformService.ToLowerCase);

    [RelayCommand]
    private void ToTitleCase() => ApplyTransform(_transformService.ToTitleCase);

    [RelayCommand]
    private void ToggleCase() => ApplyTransform(_transformService.ToggleCase);

    [RelayCommand]
    private void TrimLines() => ApplyTransform(_transformService.TrimLines);

    [RelayCommand]
    private void RemoveEmptyLines() => ApplyTransform(_transformService.RemoveEmptyLines);

    [RelayCommand]
    private void FormatJson() => ApplyTransform(_transformService.FormatJson);

    [RelayCommand]
    private void MinifyJson() => ApplyTransform(_transformService.MinifyJson);

    [RelayCommand]
    private void FormatXml() => ApplyTransform(_transformService.FormatXml);

    [RelayCommand]
    private void EncodeBase64() => ApplyTransform(_transformService.EncodeBase64);

    [RelayCommand]
    private void DecodeBase64() => ApplyTransform(_transformService.DecodeBase64);

    [RelayCommand]
    private void EncodeUrl() => ApplyTransform(_transformService.EncodeUrl);

    [RelayCommand]
    private void DecodeUrl() => ApplyTransform(_transformService.DecodeUrl);

    [RelayCommand]
    private void ToggleComment()
    {
        var tab = _mainViewModel.ActiveTab;
        if (tab == null) return;
        tab.Content = _commentService.ToggleComment(tab.Content, tab.Language);
    }
}
