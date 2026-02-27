using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using NotepadCommander.UI.ViewModels.Dialogs;

namespace NotepadCommander.UI.Services;

public class DialogService : IDialogService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task<bool> ShowDialogAsync(ModalDialogViewModelBase viewModel)
    {
        return await viewModel.ShowDialogAsync();
    }

    public async Task<TResult?> ShowDialogAsync<TResult>(ModalDialogViewModelBase<TResult> viewModel)
    {
        return await viewModel.ShowDialogAsync();
    }

    public async Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "")
    {
        var parent = GetMainWindow();
        if (parent == null) return null;

        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? result = null;
        var textBox = new TextBox { Text = defaultValue, Margin = new Thickness(10, 5) };
        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 5, 10, 10)
        };
        var cancelButton = new Button
        {
            Content = "Annuler",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 5, 5, 10)
        };

        okButton.Click += (_, _) => { result = textBox.Text; dialog.Close(); };
        cancelButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(10),
            Children =
            {
                new TextBlock { Text = message, Margin = new Thickness(10, 10, 10, 5) },
                textBox,
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { cancelButton, okButton }
                }
            }
        };

        await dialog.ShowDialog(parent);
        return result;
    }

    public async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        var parent = GetMainWindow();
        if (parent == null) return false;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        bool result = false;
        var okButton = new Button
        {
            Content = "Oui",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 5, 10, 10)
        };
        var cancelButton = new Button
        {
            Content = "Non",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 5, 5, 10)
        };

        okButton.Click += (_, _) => { result = true; dialog.Close(); };
        cancelButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(10),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(10, 10, 10, 5),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { cancelButton, okButton }
                }
            }
        };

        await dialog.ShowDialog(parent);
        return result;
    }

    public async Task<IReadOnlyList<IStorageFile>> ShowOpenFileDialogAsync(
        string title,
        IReadOnlyList<FilePickerFileType>? filters = null,
        bool allowMultiple = true)
    {
        var parent = GetMainWindow();
        if (parent == null) return Array.Empty<IStorageFile>();

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = filters
        };

        return await parent.StorageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFile?> ShowSaveFileDialogAsync(
        string title,
        string? suggestedFileName = null,
        string? defaultExtension = null,
        IReadOnlyList<FilePickerFileType>? filters = null)
    {
        var parent = GetMainWindow();
        if (parent == null) return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = defaultExtension,
            FileTypeChoices = filters
        };

        return await parent.StorageProvider.SaveFilePickerAsync(options);
    }

    public async Task<IReadOnlyList<IStorageFolder>> ShowOpenFolderDialogAsync(string title, bool allowMultiple = false)
    {
        var parent = GetMainWindow();
        if (parent == null) return Array.Empty<IStorageFolder>();

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple
        };

        return await parent.StorageProvider.OpenFolderPickerAsync(options);
    }

    public async Task ShowFileChangedDialogAsync(string message, Func<Task> onReload)
    {
        var confirmed = await ShowConfirmDialogAsync("Fichier modifie", message);
        if (confirmed)
            await onReload();
    }
}
