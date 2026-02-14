using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using NotepadCommander.Core.Services.Git;

namespace NotepadCommander.UI.Controls;

public class GitGutterMargin : AbstractMargin
{
    private const double MarginWidth = 4;

    private IReadOnlyList<GitLineChange> _changes = Array.Empty<GitLineChange>();

    private static readonly IBrush AddedBrush = new SolidColorBrush(Color.Parse("#4EC94E"));
    private static readonly IBrush ModifiedBrush = new SolidColorBrush(Color.Parse("#1B80C4"));
    private static readonly IBrush DeletedBrush = new SolidColorBrush(Color.Parse("#E05D44"));

    public void UpdateChanges(IReadOnlyList<GitLineChange> changes)
    {
        _changes = changes;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(MarginWidth, 0);
    }

    public override void Render(DrawingContext drawingContext)
    {
        if (TextView == null || !TextView.VisualLinesValid) return;

        var renderSize = Bounds.Size;
        drawingContext.FillRectangle(Brushes.Transparent, new Rect(0, 0, renderSize.Width, renderSize.Height));

        foreach (var visualLine in TextView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            var change = FindChange(lineNumber);
            if (change == null) continue;

            var brush = change.ChangeType switch
            {
                GitChangeType.Added => AddedBrush,
                GitChangeType.Modified => ModifiedBrush,
                GitChangeType.Deleted => DeletedBrush,
                _ => null
            };

            if (brush == null) continue;

            var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.LineTop)
                    - TextView.VerticalOffset;
            var height = visualLine.Height;

            if (change.ChangeType == GitChangeType.Deleted)
            {
                // Draw a small triangle/line for deletions
                drawingContext.FillRectangle(brush, new Rect(0, y, MarginWidth, 2));
            }
            else
            {
                drawingContext.FillRectangle(brush, new Rect(0, y, MarginWidth, height));
            }
        }
    }

    private GitLineChange? FindChange(int lineNumber)
    {
        foreach (var change in _changes)
        {
            if (change.LineNumber == lineNumber)
                return change;
        }
        return null;
    }
}
