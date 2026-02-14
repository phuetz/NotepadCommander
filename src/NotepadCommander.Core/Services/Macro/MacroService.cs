using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Macro;

public class MacroService : IMacroService
{
    private readonly List<MacroStep> _currentSteps = new();
    private readonly List<Models.Macro> _savedMacros = new();
    private Models.Macro? _lastRecording;

    public bool IsRecording { get; private set; }

    public void StartRecording()
    {
        _currentSteps.Clear();
        IsRecording = true;
    }

    public void StopRecording()
    {
        IsRecording = false;
        _lastRecording = new Models.Macro
        {
            Name = $"Macro {DateTime.Now:HH:mm:ss}",
            Steps = new List<MacroStep>(_currentSteps)
        };
    }

    public void RecordStep(MacroStep step)
    {
        if (IsRecording)
            _currentSteps.Add(step);
    }

    public Models.Macro? GetLastRecording() => _lastRecording;

    public List<Models.Macro> GetSavedMacros() => new(_savedMacros);

    public void SaveMacro(string name)
    {
        if (_lastRecording == null) return;

        var existing = _savedMacros.FindIndex(m => m.Name == name);
        var macro = new Models.Macro
        {
            Name = name,
            Steps = new List<MacroStep>(_lastRecording.Steps)
        };

        if (existing >= 0)
            _savedMacros[existing] = macro;
        else
            _savedMacros.Add(macro);
    }

    public void DeleteMacro(string name)
    {
        _savedMacros.RemoveAll(m => m.Name == name);
    }
}
