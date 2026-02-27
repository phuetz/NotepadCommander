using System.Reflection;

namespace NotepadCommander.UI.ViewModels.Dialogs;

public class AboutDialogViewModel : ModalDialogViewModelBase
{
    public string VersionText { get; }

    public AboutDialogViewModel()
    {
        DialogTitle = "A propos";
        OkButtonText = "Fermer";
        ShowCancelButton = false;

        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        VersionText = version != null
            ? $"Version {version.Major}.{version.Minor}.{version.Build}"
            : "Version 1.0.0";
    }
}
