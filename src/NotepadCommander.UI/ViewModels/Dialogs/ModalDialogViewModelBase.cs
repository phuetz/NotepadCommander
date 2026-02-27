using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NotepadCommander.UI.ViewModels.Dialogs;

/// <summary>
/// Base class for modal dialogs using TaskCompletionSource for async results.
/// Inspired by AvalonStudio's ModalDialogViewModelBase pattern.
/// Usage: var result = await dialogService.ShowDialogAsync(new MyDialogViewModel());
/// </summary>
public abstract partial class ModalDialogViewModelBase : ViewModelBase
{
    private TaskCompletionSource<bool>? _dialogCompletion;

    [ObservableProperty]
    private string dialogTitle = string.Empty;

    [ObservableProperty]
    private bool isDialogVisible;

    [ObservableProperty]
    private bool showOkButton = true;

    [ObservableProperty]
    private bool showCancelButton = true;

    [ObservableProperty]
    private string okButtonText = "OK";

    [ObservableProperty]
    private string cancelButtonText = "Annuler";

    public Task<bool> ShowDialogAsync()
    {
        _dialogCompletion = new TaskCompletionSource<bool>();
        IsDialogVisible = true;
        OnDialogOpened();
        return _dialogCompletion.Task;
    }

    [RelayCommand]
    protected virtual void Ok()
    {
        CloseDialog(true);
    }

    [RelayCommand]
    protected virtual void Cancel()
    {
        CloseDialog(false);
    }

    protected void CloseDialog(bool result)
    {
        IsDialogVisible = false;
        OnDialogClosed();
        _dialogCompletion?.TrySetResult(result);
    }

    protected virtual void OnDialogOpened() { }
    protected virtual void OnDialogClosed() { }
}

/// <summary>
/// Generic version that returns a typed result.
/// </summary>
public abstract partial class ModalDialogViewModelBase<TResult> : ViewModelBase
{
    private TaskCompletionSource<TResult?>? _dialogCompletion;

    [ObservableProperty]
    private string dialogTitle = string.Empty;

    [ObservableProperty]
    private bool isDialogVisible;

    [ObservableProperty]
    private bool showOkButton = true;

    [ObservableProperty]
    private bool showCancelButton = true;

    [ObservableProperty]
    private string okButtonText = "OK";

    [ObservableProperty]
    private string cancelButtonText = "Annuler";

    public Task<TResult?> ShowDialogAsync()
    {
        _dialogCompletion = new TaskCompletionSource<TResult?>();
        IsDialogVisible = true;
        OnDialogOpened();
        return _dialogCompletion.Task;
    }

    [RelayCommand]
    protected virtual void Ok()
    {
        if (!CanOk()) return;
        CloseDialog(GetResult());
    }

    [RelayCommand]
    protected virtual void Cancel()
    {
        CloseDialog(default);
    }

    /// <summary>
    /// Complete the dialog with a specific result (for custom buttons).
    /// </summary>
    protected void Complete(TResult result)
    {
        IsDialogVisible = false;
        OnDialogClosed();
        _dialogCompletion?.TrySetResult(result);
    }

    protected void CloseDialog(TResult? result)
    {
        IsDialogVisible = false;
        OnDialogClosed();
        _dialogCompletion?.TrySetResult(result);
    }

    /// <summary>
    /// Override to provide the typed result when OK is clicked.
    /// </summary>
    protected abstract TResult? GetResult();

    /// <summary>
    /// Override to validate before OK. Return false to prevent closing.
    /// </summary>
    protected virtual bool CanOk() => true;

    protected virtual void OnDialogOpened() { }
    protected virtual void OnDialogClosed() { }
}
