using AvaloniaEdit;
using NotepadCommander.UI.Controls;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.UI.Services;

public class LineOperationsHandler
{
    private readonly TextEditor _textEditor;

    public LineOperationsHandler(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    public void DuplicateLine(DocumentTabViewModel? vm)
    {
        if (vm == null) return;
        var doc = _textEditor.Document;
        var line = doc.GetLineByNumber(_textEditor.TextArea.Caret.Line);
        var lineText = doc.GetText(line.Offset, line.Length);
        doc.Insert(line.EndOffset, Environment.NewLine + lineText);
        vm.Content = _textEditor.Text ?? string.Empty;
    }

    public void DeleteLine(DocumentTabViewModel? vm)
    {
        if (vm == null) return;
        var doc = _textEditor.Document;
        var line = doc.GetLineByNumber(_textEditor.TextArea.Caret.Line);
        doc.Remove(line.Offset, line.TotalLength);
        vm.Content = _textEditor.Text ?? string.Empty;
    }

    public void MoveLine(bool up, DocumentTabViewModel? vm)
    {
        if (vm == null) return;
        var doc = _textEditor.Document;
        var caret = _textEditor.TextArea.Caret;
        var lineNum = caret.Line;

        if (up && lineNum <= 1) return;
        if (!up && lineNum >= doc.LineCount) return;

        var currentLine = doc.GetLineByNumber(lineNum);
        var targetLineNum = up ? lineNum - 1 : lineNum + 1;
        var targetLine = doc.GetLineByNumber(targetLineNum);

        var currentText = doc.GetText(currentLine.Offset, currentLine.Length);
        var targetText = doc.GetText(targetLine.Offset, targetLine.Length);

        doc.BeginUpdate();
        if (up)
        {
            doc.Replace(targetLine.Offset, targetLine.Length, currentText);
            var newCurrent = doc.GetLineByNumber(lineNum);
            doc.Replace(newCurrent.Offset, newCurrent.Length, targetText);
        }
        else
        {
            doc.Replace(currentLine.Offset, currentLine.Length, targetText);
            var newTarget = doc.GetLineByNumber(targetLineNum);
            doc.Replace(newTarget.Offset, newTarget.Length, currentText);
        }
        doc.EndUpdate();

        caret.Line = targetLineNum;
        _textEditor.ScrollTo(caret.Line, caret.Column);
        vm.Content = _textEditor.Text ?? string.Empty;
    }

    public void GoToMatchingBracket()
    {
        var (open, close) = BracketHighlightRenderer.FindMatchingBracket(
            _textEditor.Document, _textEditor.CaretOffset);
        if (open < 0 || close < 0) return;

        var target = Math.Abs(_textEditor.CaretOffset - open) <= 1 ? close + 1 : open + 1;
        _textEditor.CaretOffset = target;
        var loc = _textEditor.Document.GetLocation(target);
        _textEditor.ScrollTo(loc.Line, loc.Column);
    }
}
