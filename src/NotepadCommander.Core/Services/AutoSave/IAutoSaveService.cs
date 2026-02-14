namespace NotepadCommander.Core.Services.AutoSave;

public interface IAutoSaveService
{
    bool IsEnabled { get; set; }
    int IntervalSeconds { get; set; }
    void Start(Func<Task> saveAction);
    void Stop();
}
