using Avalonia.Controls;
using Avalonia.Interactivity;
using NotepadCommander.Core.Models;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
    }

    private void OnEncodingSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag } && DataContext is ShellViewModel vm && vm.TabManager.ActiveTab != null)
        {
            if (Enum.TryParse<DocumentEncoding>(tag, out var encoding))
                vm.TabManager.ActiveTab.Encoding = encoding;
        }
    }

    private void OnLineEndingSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag } && DataContext is ShellViewModel vm && vm.TabManager.ActiveTab != null)
        {
            if (Enum.TryParse<LineEndingType>(tag, out var lineEnding))
                vm.TabManager.ActiveTab.LineEnding = lineEnding;
        }
    }

    private void OnLanguageSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag } && DataContext is ShellViewModel vm && vm.TabManager.ActiveTab != null)
        {
            if (Enum.TryParse<SupportedLanguage>(tag, out var language))
                vm.TabManager.ActiveTab.Language = language;
        }
    }
}
