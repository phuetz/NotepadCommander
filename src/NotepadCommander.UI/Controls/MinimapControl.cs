using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;

namespace NotepadCommander.UI.Controls;

public class MinimapControl : Control
{
    private const double MinimapWidth = 80;
    private const double LineHeight = 2;
    private const double MinLineHeight = 1;

    private TextEditor? _editor;
    private string[] _lines = Array.Empty<string>();
    private double _viewportTop;
    private double _viewportHeight;
    private double _totalDocHeight;
    private bool _isDragging;
    private DispatcherTimer? _debounceTimer;

    public MinimapControl()
    {
        Width = MinimapWidth;
        ClipToBounds = true;
        IsHitTestVisible = true;

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            InvalidateVisual();
        };
    }

    public void AttachEditor(TextEditor editor)
    {
        if (_editor != null)
            DetachEditor();

        _editor = editor;
        _editor.TextChanged += OnTextChanged;
        _editor.TextArea.TextView.ScrollOffsetChanged += OnScrollChanged;
        UpdateLines();
        InvalidateVisual();
    }

    public void DetachEditor()
    {
        if (_editor == null) return;
        _editor.TextChanged -= OnTextChanged;
        _editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollChanged;
        _editor = null;
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Start();
        UpdateLines();
    }

    private void OnScrollChanged(object? sender, EventArgs e)
    {
        UpdateViewport();
        InvalidateVisual();
    }

    private void UpdateLines()
    {
        if (_editor == null) return;
        _lines = (_editor.Text ?? string.Empty).Split('\n');
    }

    private void UpdateViewport()
    {
        if (_editor == null) return;

        var textView = _editor.TextArea.TextView;
        var scrollOffset = textView.ScrollOffset;
        var docHeight = textView.DocumentHeight;
        var viewHeight = textView.Bounds.Height;

        if (docHeight <= 0) return;

        _totalDocHeight = _lines.Length * LineHeight;
        var scale = Bounds.Height / Math.Max(_totalDocHeight, Bounds.Height);

        _viewportTop = (scrollOffset.Y / docHeight) * Bounds.Height;
        _viewportHeight = Math.Max(10, (viewHeight / docHeight) * Bounds.Height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Background
        context.FillRectangle(new SolidColorBrush(Color.Parse("#F5F5F5")), new Rect(Bounds.Size));

        if (_lines.Length == 0) return;

        var actualLineHeight = Math.Max(MinLineHeight,
            Math.Min(LineHeight, Bounds.Height / _lines.Length));

        // Draw lines
        for (var i = 0; i < _lines.Length; i++)
        {
            var line = _lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var y = i * actualLineHeight;
            if (y > Bounds.Height) break;

            var brush = GetLineBrush(line);
            var trimmedLength = Math.Min(line.TrimEnd().Length, 120);
            var width = Math.Max(2, trimmedLength * MinimapWidth / 120.0);

            var indent = 0;
            foreach (var ch in line)
            {
                if (ch == ' ') indent++;
                else if (ch == '\t') indent += 4;
                else break;
            }
            var x = indent * MinimapWidth / 120.0;

            context.FillRectangle(brush,
                new Rect(x, y, Math.Min(width, MinimapWidth - x), Math.Max(actualLineHeight - 0.5, 0.5)));
        }

        // Draw viewport indicator
        UpdateViewport();
        var viewportBrush = new SolidColorBrush(Color.Parse("#200080FF"));
        var viewportBorder = new Pen(new SolidColorBrush(Color.Parse("#400080FF")), 1);
        var viewportRect = new Rect(0, _viewportTop, MinimapWidth, _viewportHeight);
        context.FillRectangle(viewportBrush, viewportRect);
        context.DrawRectangle(viewportBorder, viewportRect);
    }

    private static IBrush GetLineBrush(string line)
    {
        var trimmed = line.TrimStart();

        // Comments
        if (trimmed.StartsWith("//") || trimmed.StartsWith("#") || trimmed.StartsWith("--") ||
            trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            return new SolidColorBrush(Color.Parse("#80808080")); // gray

        // Strings
        if (trimmed.StartsWith("\"") || trimmed.StartsWith("'") || trimmed.StartsWith("`"))
            return new SolidColorBrush(Color.Parse("#80608B4E")); // green

        // Keywords (common across languages)
        var keywords = new[] { "class ", "interface ", "struct ", "enum ", "namespace ", "using ",
            "import ", "public ", "private ", "protected ", "function ", "def ", "fn ",
            "if ", "else ", "for ", "while ", "switch ", "case ", "return ", "async ",
            "var ", "let ", "const ", "static ", "void " };

        foreach (var kw in keywords)
        {
            if (trimmed.StartsWith(kw, StringComparison.Ordinal))
                return new SolidColorBrush(Color.Parse("#80569CD6")); // blue
        }

        return new SolidColorBrush(Color.Parse("#60333333")); // default dark
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        e.Pointer.Capture(this);
        NavigateToPosition(e.GetPosition(this).Y);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            NavigateToPosition(e.GetPosition(this).Y);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void NavigateToPosition(double y)
    {
        if (_editor == null || _lines.Length == 0) return;

        var ratio = Math.Clamp(y / Bounds.Height, 0, 1);
        var line = (int)(ratio * _lines.Length) + 1;
        line = Math.Clamp(line, 1, _lines.Length);

        _editor.ScrollTo(line, 1);
    }
}
