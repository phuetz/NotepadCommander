using System.Windows.Input;

namespace NotepadCommander.UI.Models;

/// <summary>
/// Model for a toolbar button item. Used for data-driven toolbar generation.
/// </summary>
public class ToolbarItem
{
    public string Label { get; init; } = string.Empty;
    public string IconData { get; init; } = string.Empty;
    public string? ToolTip { get; init; }
    public string? Group { get; init; }
    public ICommand? Command { get; init; }
    public object? CommandParameter { get; init; }
    public bool IsToggle { get; init; }
    public bool IsToggled { get; set; }
    public bool IsLarge { get; init; }
}
