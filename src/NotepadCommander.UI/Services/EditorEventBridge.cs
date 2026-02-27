using Avalonia.Controls;
using NotepadCommander.UI.Controls;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Services;

/// <summary>
/// Bridges ShellViewModel events to NotepadEditor actions.
/// Simplified: NotepadEditor now handles line operations and dev features directly.
/// </summary>
public class EditorEventBridge
{
    private ShellViewModel? _mainViewModel;
    private NotepadEditor? _editor;
    private Func<DocumentTabViewModel?>? _getCurrentVm;
    private Action<bool>? _setUpdatingFlag;
    private Func<bool>? _getUpdatingFlag;

    // Ctrl+D: track previous word for progressive selection
    private string? _lastCtrlDWord;
    private int _lastCtrlDIndex;

    public void Initialize(
        NotepadEditor editor,
        Func<DocumentTabViewModel?> getCurrentVm,
        Action<bool> setUpdatingFlag,
        Func<bool> getUpdatingFlag)
    {
        _editor = editor;
        _getCurrentVm = getCurrentVm;
        _setUpdatingFlag = setUpdatingFlag;
        _getUpdatingFlag = getUpdatingFlag;
    }

    public void Connect(ShellViewModel mainVm)
    {
        if (_mainViewModel != null)
            Disconnect();

        _mainViewModel = mainVm;

        _mainViewModel.UndoRequested += OnUndo;
        _mainViewModel.RedoRequested += OnRedo;
        _mainViewModel.CutRequested += OnCut;
        _mainViewModel.CopyRequested += OnCopy;
        _mainViewModel.PasteRequested += OnPaste;
        _mainViewModel.SelectionRequested += OnSelectionRequested;
        _mainViewModel.ToggleCommentRequested += OnToggleComment;
        _mainViewModel.SelectWordAndHighlightRequested += OnSelectWordAndHighlight;
        _mainViewModel.DuplicateLineRequested += OnDuplicateLine;
        _mainViewModel.DeleteLineRequested += OnDeleteLine;
        _mainViewModel.MoveLineRequested += OnMoveLine;
        _mainViewModel.GoToMatchingBracketRequested += OnGoToMatchingBracket;
    }

    public void Disconnect()
    {
        if (_mainViewModel == null) return;
        _mainViewModel.UndoRequested -= OnUndo;
        _mainViewModel.RedoRequested -= OnRedo;
        _mainViewModel.CutRequested -= OnCut;
        _mainViewModel.CopyRequested -= OnCopy;
        _mainViewModel.PasteRequested -= OnPaste;
        _mainViewModel.SelectionRequested -= OnSelectionRequested;
        _mainViewModel.ToggleCommentRequested -= OnToggleComment;
        _mainViewModel.SelectWordAndHighlightRequested -= OnSelectWordAndHighlight;
        _mainViewModel.DuplicateLineRequested -= OnDuplicateLine;
        _mainViewModel.DeleteLineRequested -= OnDeleteLine;
        _mainViewModel.MoveLineRequested -= OnMoveLine;
        _mainViewModel.GoToMatchingBracketRequested -= OnGoToMatchingBracket;
        _mainViewModel = null;
    }

    private void OnUndo()
    {
        if (_editor?.Document.UndoStack.CanUndo == true)
            _editor.Document.UndoStack.Undo();
    }

    private void OnRedo()
    {
        if (_editor?.Document.UndoStack.CanRedo == true)
            _editor.Document.UndoStack.Redo();
    }

    private void OnCut()
    {
        if (_editor == null) return;
        if (!string.IsNullOrEmpty(_editor.SelectedText))
            _mainViewModel?.AddToClipboardHistory(_editor.SelectedText);
        _editor.Cut();
    }

    private void OnCopy()
    {
        if (_editor == null) return;
        if (!string.IsNullOrEmpty(_editor.SelectedText))
            _mainViewModel?.AddToClipboardHistory(_editor.SelectedText);
        _editor.Copy();
    }

    private async void OnPaste()
    {
        if (_editor == null) return;
        var clipboard = TopLevel.GetTopLevel(_editor)?.Clipboard;
        if (clipboard == null) return;
        var text = await clipboard.GetTextAsync();
        if (text != null)
            _editor.TextArea.Selection.ReplaceSelectionWithText(text);
    }

    private void OnSelectionRequested(int offset, int length)
    {
        if (_editor == null) return;
        _editor.Select(offset, length);
        var loc = _editor.Document.GetLocation(offset);
        _editor.ScrollTo(loc.Line, loc.Column);
    }

    private void OnToggleComment(string _)
    {
        var vm = _getCurrentVm?.Invoke();
        if (_editor == null || vm == null || _mainViewModel == null) return;

        var textArea = _editor.TextArea;
        var doc = _editor.Document;

        int startLine, endLine;
        if (textArea.Selection.IsEmpty)
        {
            startLine = endLine = textArea.Caret.Line;
        }
        else
        {
            var selStart = textArea.Selection.SurroundingSegment;
            if (selStart == null) return;
            startLine = doc.GetLineByOffset(selStart.Offset).LineNumber;
            endLine = doc.GetLineByOffset(selStart.EndOffset).LineNumber;
        }

        var firstDocLine = doc.GetLineByNumber(startLine);
        var lastDocLine = doc.GetLineByNumber(endLine);
        var offset = firstDocLine.Offset;
        var length = lastDocLine.EndOffset - firstDocLine.Offset;
        var linesText = doc.GetText(offset, length);

        var commented = _mainViewModel.CommentService.ToggleComment(linesText, vm.Language);

        _setUpdatingFlag?.Invoke(true);
        doc.Replace(offset, length, commented);
        _setUpdatingFlag?.Invoke(false);

        vm.Content = _editor.Text ?? string.Empty;
    }

    private void OnSelectWordAndHighlight()
    {
        if (_editor == null || _mainViewModel == null || _editor.OccurrenceRenderer == null) return;

        var doc = _editor.Document;
        string? word;

        // First call: select word at caret or use current selection
        if (_editor.TextArea.Selection.IsEmpty)
        {
            var (start, end) = OccurrenceHighlightRenderer.GetWordBoundsAtOffset(doc, _editor.CaretOffset);
            if (start < 0) return;
            _editor.Select(start, end - start);
            word = doc.GetText(start, end - start);
            _lastCtrlDWord = word;
            _lastCtrlDIndex = end;
        }
        else
        {
            word = _editor.SelectedText;

            // Same word as before? Find next occurrence
            if (word == _lastCtrlDWord)
            {
                var nextIndex = doc.Text.IndexOf(word, _lastCtrlDIndex, StringComparison.Ordinal);
                if (nextIndex < 0)
                    nextIndex = doc.Text.IndexOf(word, 0, StringComparison.Ordinal); // wrap around

                if (nextIndex >= 0)
                {
                    _editor.Select(nextIndex, word.Length);
                    _editor.ScrollTo(doc.GetLocation(nextIndex).Line, doc.GetLocation(nextIndex).Column);
                    _lastCtrlDIndex = nextIndex + word.Length;
                }
            }
            else
            {
                // New word selected
                _lastCtrlDWord = word;
                _lastCtrlDIndex = _editor.SelectionStart + _editor.SelectionLength;
            }
        }

        if (string.IsNullOrEmpty(word)) return;

        var occurrences = OccurrenceHighlightRenderer.FindAllOccurrences(doc, word);
        _editor.OccurrenceRenderer.SetOccurrences(occurrences);
        _editor.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);

        _mainViewModel.FindReplaceViewModel.SearchText = word;
        _mainViewModel.FindReplaceViewModel.Show(true);
    }

    private void OnDuplicateLine()
    {
        if (_editor == null) return;
        _editor.DuplicateLine();
        var vm = _getCurrentVm?.Invoke();
        if (vm != null) vm.Content = _editor.Text ?? string.Empty;
    }

    private void OnDeleteLine()
    {
        if (_editor == null) return;
        _editor.DeleteLine();
        var vm = _getCurrentVm?.Invoke();
        if (vm != null) vm.Content = _editor.Text ?? string.Empty;
    }

    private void OnMoveLine(bool up)
    {
        if (_editor == null) return;
        if (up) _editor.MoveLineUp(); else _editor.MoveLineDown();
        var vm = _getCurrentVm?.Invoke();
        if (vm != null) vm.Content = _editor.Text ?? string.Empty;
    }

    private void OnGoToMatchingBracket()
    {
        _editor?.GoToMatchingBracket();
    }
}
