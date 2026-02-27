using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.UI.Views.Dialogs;

namespace NotepadCommander.UI.ViewModels.Dialogs;

public partial class SavePromptDialogViewModel : ModalDialogViewModelBase<SavePromptResult>
{
    [ObservableProperty]
    private string message;

    public SavePromptDialogViewModel(string? fileName = null)
    {
        DialogTitle = "Enregistrer les modifications";
        ShowOkButton = false;
        ShowCancelButton = false;
        Message = string.IsNullOrEmpty(fileName)
            ? "Voulez-vous enregistrer les modifications ?"
            : $"Voulez-vous enregistrer les modifications apportees a {fileName} ?";
    }

    [RelayCommand]
    private void Save() => Complete(SavePromptResult.Save);

    [RelayCommand]
    private void DontSave() => Complete(SavePromptResult.DontSave);

    [RelayCommand]
    private void CancelDialog() => Complete(SavePromptResult.Cancel);

    protected override SavePromptResult GetResult() => SavePromptResult.Cancel;
}
