using Xunit;
using NotepadCommander.Core.Services.Macro;
using NotepadCommander.Core.Models;

namespace NotepadCommander.Tests;

public class MacroServiceTests
{
    private readonly MacroService _service = new();

    [Fact]
    public void IsRecording_DefaultFalse()
    {
        Assert.False(_service.IsRecording);
    }

    [Fact]
    public void StartRecording_SetsIsRecording()
    {
        _service.StartRecording();
        Assert.True(_service.IsRecording);
    }

    [Fact]
    public void StopRecording_ClearsIsRecording()
    {
        _service.StartRecording();
        _service.StopRecording();
        Assert.False(_service.IsRecording);
    }

    [Fact]
    public void RecordStep_WhileRecording_AddsStep()
    {
        _service.StartRecording();
        _service.RecordStep(new MacroStep { Action = MacroAction.InsertText, Value = "hello" });
        _service.StopRecording();

        var macro = _service.GetLastRecording();
        Assert.NotNull(macro);
        Assert.Single(macro.Steps);
        Assert.Equal("hello", macro.Steps[0].Value);
    }

    [Fact]
    public void RecordStep_WhenNotRecording_Ignored()
    {
        _service.RecordStep(new MacroStep { Action = MacroAction.InsertText, Value = "test" });
        Assert.Null(_service.GetLastRecording());
    }

    [Fact]
    public void SaveMacro_PersistsToList()
    {
        _service.StartRecording();
        _service.RecordStep(new MacroStep { Action = MacroAction.InsertText, Value = "x" });
        _service.StopRecording();
        _service.SaveMacro("TestMacro");

        var macros = _service.GetSavedMacros();
        Assert.Single(macros);
        Assert.Equal("TestMacro", macros[0].Name);
    }

    [Fact]
    public void DeleteMacro_RemovesFromList()
    {
        _service.StartRecording();
        _service.RecordStep(new MacroStep { Action = MacroAction.InsertText, Value = "x" });
        _service.StopRecording();
        _service.SaveMacro("ToDelete");
        _service.DeleteMacro("ToDelete");

        Assert.Empty(_service.GetSavedMacros());
    }

    [Fact]
    public void SaveMacro_OverwritesExisting()
    {
        _service.StartRecording();
        _service.RecordStep(new MacroStep { Action = MacroAction.InsertText, Value = "v1" });
        _service.StopRecording();
        _service.SaveMacro("Macro1");

        _service.StartRecording();
        _service.RecordStep(new MacroStep { Action = MacroAction.InsertText, Value = "v2" });
        _service.StopRecording();
        _service.SaveMacro("Macro1");

        var macros = _service.GetSavedMacros();
        Assert.Single(macros);
        Assert.Equal("v2", macros[0].Steps[0].Value);
    }
}
