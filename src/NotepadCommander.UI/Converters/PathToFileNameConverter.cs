using System.Globalization;
using Avalonia.Data.Converters;

namespace NotepadCommander.UI.Converters;

public class PathToFileNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
            return Path.GetFileName(path);
        return "Sans titre";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
