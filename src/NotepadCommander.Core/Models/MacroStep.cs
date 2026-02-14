namespace NotepadCommander.Core.Models;

public class MacroStep
{
    public MacroAction Action { get; set; }
    public string? Value { get; set; }
    public int Position { get; set; }
}

public enum MacroAction
{
    InsertText,
    DeleteText,
    MoveCursor,
    SelectText,
    ReplaceText
}

public class Macro
{
    public string Name { get; set; } = string.Empty;
    public List<MacroStep> Steps { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
