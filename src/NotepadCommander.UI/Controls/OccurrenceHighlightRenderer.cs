using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace NotepadCommander.UI.Controls;

public class OccurrenceHighlightRenderer : IBackgroundRenderer
{
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.Parse("#40FFFF00"));
    private static readonly IPen HighlightPen = new Pen(new SolidColorBrush(Color.Parse("#80FFD700")), 1);

    private readonly List<(int offset, int length)> _occurrences = new();

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetOccurrences(IEnumerable<(int offset, int length)> occurrences)
    {
        _occurrences.Clear();
        _occurrences.AddRange(occurrences);
    }

    public void ClearOccurrences()
    {
        _occurrences.Clear();
    }

    public int Count => _occurrences.Count;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_occurrences.Count == 0) return;

        foreach (var (offset, length) in _occurrences)
        {
            if (offset < 0 || offset + length > textView.Document.TextLength) continue;

            var segment = new TextSegment { StartOffset = offset, Length = length };
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.FillRectangle(HighlightBrush, rect);
                drawingContext.DrawRectangle(HighlightPen, rect);
            }
        }
    }

    public static string? GetWordAtOffset(TextDocument document, int offset)
    {
        if (offset < 0 || offset >= document.TextLength) return null;

        var start = offset;
        var end = offset;

        while (start > 0 && IsWordChar(document.GetCharAt(start - 1)))
            start--;

        while (end < document.TextLength && IsWordChar(document.GetCharAt(end)))
            end++;

        if (start == end) return null;
        return document.GetText(start, end - start);
    }

    public static (int start, int end) GetWordBoundsAtOffset(TextDocument document, int offset)
    {
        if (offset < 0 || offset >= document.TextLength) return (-1, -1);

        var start = offset;
        var end = offset;

        while (start > 0 && IsWordChar(document.GetCharAt(start - 1)))
            start--;

        while (end < document.TextLength && IsWordChar(document.GetCharAt(end)))
            end++;

        if (start == end) return (-1, -1);
        return (start, end);
    }

    public static List<(int offset, int length)> FindAllOccurrences(TextDocument document, string word)
    {
        var results = new List<(int offset, int length)>();
        if (string.IsNullOrEmpty(word)) return results;

        var text = document.Text;
        var index = 0;
        while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
        {
            // Check word boundaries
            var before = index > 0 ? text[index - 1] : ' ';
            var after = index + word.Length < text.Length ? text[index + word.Length] : ' ';

            if (!IsWordChar(before) && !IsWordChar(after))
            {
                results.Add((index, word.Length));
            }
            index += word.Length;
        }
        return results;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
