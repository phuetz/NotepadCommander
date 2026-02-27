using CommunityToolkit.Mvvm.ComponentModel;

namespace NotepadCommander.UI.ViewModels.Dialogs;

public partial class GoToLineDialogViewModel : ModalDialogViewModelBase<int?>
{
    private readonly int _maxLine;

    [ObservableProperty]
    private string lineInput = string.Empty;

    [ObservableProperty]
    private string? errorMessage;

    public string WatermarkText { get; }

    public GoToLineDialogViewModel(int maxLine)
    {
        _maxLine = maxLine;
        DialogTitle = "Aller a la ligne";
        OkButtonText = "Aller";
        WatermarkText = $"Numero de ligne (1-{maxLine})";
    }

    protected override int? GetResult()
    {
        if (int.TryParse(LineInput, out var line) && line >= 1 && line <= _maxLine)
            return line;
        return null;
    }

    protected override bool CanOk()
    {
        if (!int.TryParse(LineInput, out var line))
        {
            ErrorMessage = "Entrez un nombre valide";
            return false;
        }
        if (line < 1 || line > _maxLine)
        {
            ErrorMessage = $"La ligne doit etre entre 1 et {_maxLine}";
            return false;
        }
        ErrorMessage = null;
        return true;
    }
}
