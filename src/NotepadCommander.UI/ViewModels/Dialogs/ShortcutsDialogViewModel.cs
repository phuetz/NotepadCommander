namespace NotepadCommander.UI.ViewModels.Dialogs;

public class ShortcutsDialogViewModel : ModalDialogViewModelBase
{
    public record ShortcutEntry(string Keys, string Description, string? Category = null);

    public List<ShortcutEntry> Shortcuts { get; } = new()
    {
        new("Ctrl+N", "Nouveau document", "Fichier"),
        new("Ctrl+O", "Ouvrir un fichier", "Fichier"),
        new("Ctrl+S", "Enregistrer", "Fichier"),
        new("Ctrl+Shift+S", "Enregistrer sous", "Fichier"),
        new("Ctrl+W", "Fermer l'onglet", "Fichier"),

        new("Ctrl+Z", "Annuler", "Edition"),
        new("Ctrl+Y", "Refaire", "Edition"),
        new("Ctrl+D", "Selectionner mot et occurrences", "Edition"),
        new("Ctrl+Shift+D", "Dupliquer la ligne", "Edition"),
        new("Ctrl+Shift+K", "Supprimer la ligne", "Edition"),
        new("Alt+Up/Down", "Deplacer la ligne", "Edition"),
        new("Ctrl+/", "Commenter/Decommenter", "Edition"),

        new("Ctrl+F", "Rechercher", "Recherche"),
        new("Ctrl+H", "Remplacer", "Recherche"),
        new("Ctrl+Shift+F", "Rechercher dans les fichiers", "Recherche"),
        new("Ctrl+G", "Aller a la ligne", "Recherche"),

        new("Ctrl+Tab", "Onglet suivant", "Navigation"),
        new("Ctrl+Shift+Tab", "Onglet precedent", "Navigation"),
        new("Ctrl+1..5", "Onglet ruban 1-5", "Navigation"),
        new("Ctrl+B", "Crochet correspondant", "Navigation"),
        new("Ctrl+P / Ctrl+Shift+P", "Palette de commandes", "Navigation"),

        new("Ctrl+Shift+M", "Extraction de methodes", "Outils"),
        new("Ctrl+`", "Terminal", "Outils"),
        new("Ctrl+Shift+V", "Historique presse-papiers", "Outils"),
        new(@"Ctrl+\", "Diviser l'editeur", "Outils"),

        new("F11", "Plein ecran", "Affichage"),
        new("Ctrl++/-", "Zoom avant/arriere", "Affichage"),
        new("Ctrl+0", "Zoom 100%", "Affichage"),
    };

    public ShortcutsDialogViewModel()
    {
        DialogTitle = "Raccourcis clavier";
        OkButtonText = "Fermer";
        ShowCancelButton = false;
    }
}
