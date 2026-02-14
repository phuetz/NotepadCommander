using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace NotepadCommander.UI.Controls;

public class BracketHighlightRenderer : IBackgroundRenderer
{
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.Parse("#30808080"));
    private static readonly IPen HighlightPen = new Pen(new SolidColorBrush(Color.Parse("#80808080")), 1);

    private static readonly Dictionary<char, char> OpenBrackets = new()
    {
        { '(', ')' }, { '{', '}' }, { '[', ']' }
    };

    private static readonly Dictionary<char, char> CloseBrackets = new()
    {
        { ')', '(' }, { '}', '{' }, { ']', '[' }
    };

    private int _openBracketOffset = -1;
    private int _closeBracketOffset = -1;

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetHighlight(int open, int close)
    {
        _openBracketOffset = open;
        _closeBracketOffset = close;
    }

    public void ClearHighlight()
    {
        _openBracketOffset = -1;
        _closeBracketOffset = -1;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_openBracketOffset < 0 || _closeBracketOffset < 0) return;

        DrawBracketHighlight(textView, drawingContext, _openBracketOffset);
        DrawBracketHighlight(textView, drawingContext, _closeBracketOffset);
    }

    private static void DrawBracketHighlight(TextView textView, DrawingContext drawingContext, int offset)
    {
        if (offset < 0 || offset >= textView.Document.TextLength) return;

        var segment = new TextSegment { StartOffset = offset, Length = 1 };
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            drawingContext.FillRectangle(HighlightBrush, rect);
            drawingContext.DrawRectangle(HighlightPen, rect);
        }
    }

    public static (int open, int close) FindMatchingBracket(TextDocument document, int caretOffset)
    {
        if (caretOffset < 0 || caretOffset >= document.TextLength)
            return (-1, -1);

        // Check character at caret and before caret
        var offsets = new[] { caretOffset, caretOffset - 1 };

        foreach (var offset in offsets)
        {
            if (offset < 0 || offset >= document.TextLength) continue;

            var ch = document.GetCharAt(offset);

            if (OpenBrackets.TryGetValue(ch, out var closeChar))
            {
                var matchOffset = FindForward(document, offset + 1, ch, closeChar);
                if (matchOffset >= 0) return (offset, matchOffset);
            }
            else if (CloseBrackets.TryGetValue(ch, out var openChar))
            {
                var matchOffset = FindBackward(document, offset - 1, openChar, ch);
                if (matchOffset >= 0) return (matchOffset, offset);
            }
        }

        return (-1, -1);
    }

    private static int FindForward(TextDocument document, int startOffset, char open, char close)
    {
        var depth = 1;
        for (var i = startOffset; i < document.TextLength; i++)
        {
            var ch = document.GetCharAt(i);
            if (ch == open) depth++;
            else if (ch == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static int FindBackward(TextDocument document, int startOffset, char open, char close)
    {
        var depth = 1;
        for (var i = startOffset; i >= 0; i--)
        {
            var ch = document.GetCharAt(i);
            if (ch == close) depth++;
            else if (ch == open)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }
}
