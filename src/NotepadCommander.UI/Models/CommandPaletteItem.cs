namespace NotepadCommander.UI.Models;

public class CommandPaletteItem
{
    public string Label { get; }
    public string Shortcut { get; }
    private readonly Action _action;

    public CommandPaletteItem(string label, string shortcut, Action action)
    {
        Label = label;
        Shortcut = shortcut;
        _action = action;
    }

    public void Execute() => _action();
}
