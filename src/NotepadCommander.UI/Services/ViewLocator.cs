using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace NotepadCommander.UI.Services;

/// <summary>
/// Convention-based ViewLocator: FooViewModel -> FooView.
/// Searches in Views/, Views/Components/, Views/Dialogs/ namespaces.
/// Inspired by AvalonStudio's ViewLocatorDataTemplate.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null) return new TextBlock { Text = "No data" };

        var vmTypeName = data.GetType().FullName;
        if (vmTypeName is null) return new TextBlock { Text = "Unknown type" };

        // Convention: replace "ViewModel" with "View" in the type name
        var viewTypeName = vmTypeName.Replace("ViewModel", "View");

        // Try direct name match first
        var type = Type.GetType(viewTypeName);

        // Try alternate namespace mappings if direct match fails
        if (type is null)
        {
            // ViewModels/Tools/FooToolViewModel -> Views/Components/FooToolView
            var altName = viewTypeName
                .Replace(".ViewModels.Tools.", ".Views.Components.")
                .Replace(".ViewModels.Dialogs.", ".Views.Dialogs.")
                .Replace(".ViewModels.", ".Views.");
            type = Type.GetType(altName);

            // Also try Views/Components/ for ViewModels/ types
            if (type is null)
            {
                altName = viewTypeName
                    .Replace(".ViewModels.Tools.", ".Views.Components.")
                    .Replace(".ViewModels.Dialogs.", ".Views.Dialogs.")
                    .Replace(".ViewModels.", ".Views.Components.");
                type = Type.GetType(altName);
            }
        }

        if (type is not null && typeof(Control).IsAssignableFrom(type))
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor is not null)
                return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = $"Vue introuvable : {viewTypeName}" };
    }

    public bool Match(object? data) => data is ViewModels.ViewModelBase;
}
