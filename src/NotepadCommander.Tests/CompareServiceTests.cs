using Xunit;
using NotepadCommander.Core.Services.Compare;

namespace NotepadCommander.Tests;

public class CompareServiceTests
{
    private readonly CompareService _service = new();

    [Fact]
    public void Compare_IdenticalTexts_AllUnchanged()
    {
        var text = "line1\nline2\nline3";
        var result = _service.Compare(text, text);
        Assert.All(result.Lines, l => Assert.Equal(DiffLineType.Unchanged, l.Type));
    }

    [Fact]
    public void Compare_DifferentTexts_DetectsChanges()
    {
        var old = "line1\nline2\nline3";
        var @new = "line1\nmodified\nline3";
        var result = _service.Compare(old, @new);
        Assert.NotEmpty(result.Lines);
        Assert.Contains(result.Lines, l => l.Type != DiffLineType.Unchanged);
    }

    [Fact]
    public void Compare_AddedLine_DetectsInsert()
    {
        var old = "line1\nline2";
        var @new = "line1\nline2\nline3";
        var result = _service.Compare(old, @new);
        Assert.Contains(result.Lines, l => l.Type == DiffLineType.Inserted);
    }

    [Fact]
    public void Compare_RemovedLine_DetectsDelete()
    {
        var old = "line1\nline2\nline3";
        var @new = "line1\nline3";
        var result = _service.Compare(old, @new);
        Assert.Contains(result.Lines, l => l.Type == DiffLineType.Deleted);
    }

    [Fact]
    public void Compare_EmptyTexts_ReturnsEmpty()
    {
        var result = _service.Compare("", "");
        // Either empty or single unchanged empty line
        Assert.True(result.Lines.Count <= 1);
    }
}
