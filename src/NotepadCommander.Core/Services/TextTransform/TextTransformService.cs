using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace NotepadCommander.Core.Services.TextTransform;

public class TextTransformService : ITextTransformService
{
    public string SortLines(string text, bool ascending = true)
    {
        var lines = GetLines(text);
        var sorted = ascending
            ? lines.OrderBy(l => l, StringComparer.CurrentCulture)
            : lines.OrderByDescending(l => l, StringComparer.CurrentCulture);
        return string.Join(Environment.NewLine, sorted);
    }

    public string RemoveDuplicateLines(string text)
    {
        var lines = GetLines(text);
        var seen = new HashSet<string>();
        var unique = lines.Where(l => seen.Add(l));
        return string.Join(Environment.NewLine, unique);
    }

    public string ToUpperCase(string text) => text.ToUpperInvariant();

    public string ToLowerCase(string text) => text.ToLowerInvariant();

    public string ToTitleCase(string text)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text.ToLower());
    }

    public string ToggleCase(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c));
        }
        return sb.ToString();
    }

    public string TrimLines(string text)
    {
        var lines = GetLines(text).Select(l => l.Trim());
        return string.Join(Environment.NewLine, lines);
    }

    public string RemoveEmptyLines(string text)
    {
        var lines = GetLines(text).Where(l => !string.IsNullOrWhiteSpace(l));
        return string.Join(Environment.NewLine, lines);
    }

    public string JoinLines(string text, string separator = " ")
    {
        var lines = GetLines(text).Where(l => !string.IsNullOrEmpty(l));
        return string.Join(separator, lines);
    }

    public string ReverseLines(string text)
    {
        var lines = GetLines(text).AsEnumerable().Reverse();
        return string.Join(Environment.NewLine, lines);
    }

    public string EncodeBase64(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    public string DecodeBase64(string text)
    {
        try
        {
            var bytes = Convert.FromBase64String(text.Trim());
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return text; // Retourner l'original si invalide
        }
    }

    public string EncodeUrl(string text) => Uri.EscapeDataString(text);

    public string DecodeUrl(string text) => Uri.UnescapeDataString(text);

    public string FormatJson(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return text;
        }
    }

    public string MinifyJson(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(doc);
        }
        catch (JsonException)
        {
            return text;
        }
    }

    public string FormatXml(string text)
    {
        try
        {
            var xDoc = XDocument.Parse(text);
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                OmitXmlDeclaration = text.TrimStart().StartsWith("<?xml") == false
            };

            using var sw = new StringWriter();
            using var writer = XmlWriter.Create(sw, settings);
            xDoc.WriteTo(writer);
            writer.Flush();
            return sw.ToString();
        }
        catch (XmlException)
        {
            return text;
        }
    }

    private static string[] GetLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
    }
}
