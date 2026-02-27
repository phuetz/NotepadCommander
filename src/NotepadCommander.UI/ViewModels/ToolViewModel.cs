using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NotepadCommander.UI.ViewModels;

/// <summary>
/// Default docking location for tool panels.
/// Inspired by AvalonStudio's Location enum.
/// </summary>
public enum ToolLocation
{
    Left,
    Right,
    Bottom,
    Float
}

/// <summary>
/// Base class for dockable tool panels (file explorer, search, terminal, etc.).
/// Inspired by AvalonStudio's ToolViewModel pattern.
/// </summary>
public abstract partial class ToolViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private bool isSelected;

    /// <summary>
    /// Where this tool should be docked by default.
    /// </summary>
    public abstract ToolLocation DefaultLocation { get; }

    [RelayCommand]
    public void ToggleVisibility() => IsVisible = !IsVisible;

    [RelayCommand]
    public void Close() => IsVisible = false;

    /// <summary>
    /// Called when the tool is opened/shown.
    /// </summary>
    public virtual void OnOpen() { }

    /// <summary>
    /// Called when the tool is selected (focused).
    /// </summary>
    public virtual void OnSelected() { }

    /// <summary>
    /// Called when the tool is deselected.
    /// </summary>
    public virtual void OnDeselected() { }
}
