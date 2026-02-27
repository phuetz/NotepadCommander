using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NotepadCommander.Core.Services.Calculator;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.Macro;
using NotepadCommander.Core.Services.Markdown;
using NotepadCommander.Core.Services.Snippets;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Views.Components;

public partial class ToolsToolbar : UserControl
{
    public ToolsToolbar()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ShellViewModel mainVm)
        {
            DataContext = new ToolsToolbarViewModel(
                mainVm,
                App.Services.GetRequiredService<ICompareService>(),
                App.Services.GetRequiredService<IMacroService>(),
                App.Services.GetRequiredService<ISnippetService>(),
                App.Services.GetRequiredService<IMarkdownService>(),
                App.Services.GetRequiredService<ICalculatorService>());
        }
    }
}
