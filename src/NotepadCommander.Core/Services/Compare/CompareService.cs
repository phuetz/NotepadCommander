using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace NotepadCommander.Core.Services.Compare;

public class CompareService : ICompareService
{
    public DiffResult Compare(string oldText, string newText)
    {
        var diffBuilder = new SideBySideDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText, newText);
        var result = new DiffResult();

        int oldLine = 1, newLine = 1;

        for (int i = 0; i < Math.Max(diff.OldText.Lines.Count, diff.NewText.Lines.Count); i++)
        {
            var oldDiffLine = i < diff.OldText.Lines.Count ? diff.OldText.Lines[i] : null;
            var newDiffLine = i < diff.NewText.Lines.Count ? diff.NewText.Lines[i] : null;

            if (oldDiffLine?.Type == ChangeType.Deleted)
            {
                result.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Deleted,
                    Text = oldDiffLine.Text,
                    OldLineNumber = oldLine++
                });
            }
            else if (newDiffLine?.Type == ChangeType.Inserted)
            {
                result.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Inserted,
                    Text = newDiffLine.Text,
                    NewLineNumber = newLine++
                });
            }
            else if (oldDiffLine?.Type == ChangeType.Modified)
            {
                result.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Modified,
                    Text = oldDiffLine.Text,
                    OldLineNumber = oldLine++,
                    NewLineNumber = newLine++
                });
            }
            else
            {
                var text = oldDiffLine?.Text ?? newDiffLine?.Text ?? string.Empty;
                if (oldDiffLine?.Type == ChangeType.Imaginary)
                {
                    newLine++;
                }
                else if (newDiffLine?.Type == ChangeType.Imaginary)
                {
                    oldLine++;
                }
                else
                {
                    result.Lines.Add(new DiffLine
                    {
                        Type = DiffLineType.Unchanged,
                        Text = text,
                        OldLineNumber = oldLine++,
                        NewLineNumber = newLine++
                    });
                }
            }
        }

        return result;
    }
}
