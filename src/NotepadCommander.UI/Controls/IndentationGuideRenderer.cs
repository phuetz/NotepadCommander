using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace NotepadCommander.UI.Controls;

public class IndentationGuideRenderer : IBackgroundRenderer
{
    private static readonly IPen GuidePen = CreateDashedPen();
    private int _tabSize = 4;

    public KnownLayer Layer => KnownLayer.Background;

    public int TabSize
    {
        get => _tabSize;
        set => _tabSize = Math.Max(1, value);
    }

    private static IPen CreateDashedPen()
    {
        return new Pen(
            new SolidColorBrush(Color.Parse("#30808080")),
            1,
            new DashStyle(new[] { 2.0, 2.0 }, 0));
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document == null) return;

        var charWidth = textView.WideSpaceWidth;

        foreach (var visualLine in textView.VisualLines)
        {
            var line = visualLine.FirstDocumentLine;
            var text = textView.Document.GetText(line.Offset, line.Length);

            var indentLevel = GetIndentLevel(text);
            if (indentLevel == 0) continue;

            var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.LineTop)
                    - textView.VerticalOffset;
            var height = visualLine.Height;

            for (var level = 1; level <= indentLevel; level++)
            {
                var x = charWidth * level * _tabSize;
                // Adjust for scroll offset
                x -= textView.HorizontalOffset;
                if (x < 0) continue;

                drawingContext.DrawLine(GuidePen,
                    new Point(x, y),
                    new Point(x, y + height));
            }
        }
    }

    private int GetIndentLevel(string line)
    {
        var spaces = 0;
        foreach (var ch in line)
        {
            if (ch == ' ') spaces++;
            else if (ch == '\t') spaces += _tabSize;
            else break;
        }
        return spaces / _tabSize;
    }
}
