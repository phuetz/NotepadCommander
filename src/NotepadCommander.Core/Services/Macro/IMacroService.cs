using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Macro;

public interface IMacroService
{
    bool IsRecording { get; }
    void StartRecording();
    void StopRecording();
    void RecordStep(MacroStep step);
    Models.Macro? GetLastRecording();
    List<Models.Macro> GetSavedMacros();
    void SaveMacro(string name);
    void DeleteMacro(string name);
}
