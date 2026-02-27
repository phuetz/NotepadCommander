using Avalonia.Platform.Storage;
using NotepadCommander.UI.ViewModels.Dialogs;

namespace NotepadCommander.UI.Services;

public interface IDialogService
{
    Task<bool> ShowDialogAsync(ModalDialogViewModelBase viewModel);
    Task<TResult?> ShowDialogAsync<TResult>(ModalDialogViewModelBase<TResult> viewModel);
    Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "");
    Task<bool> ShowConfirmDialogAsync(string title, string message);
    Task<IReadOnlyList<IStorageFile>> ShowOpenFileDialogAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null, bool allowMultiple = true);
    Task<IStorageFile?> ShowSaveFileDialogAsync(string title, string? suggestedFileName = null, string? defaultExtension = null, IReadOnlyList<FilePickerFileType>? filters = null);
    Task<IReadOnlyList<IStorageFolder>> ShowOpenFolderDialogAsync(string title, bool allowMultiple = false);
    Task ShowFileChangedDialogAsync(string message, Func<Task> onReload);
}
