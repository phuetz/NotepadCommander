namespace NotepadCommander.Core.Services.AutoSave;

public interface IAutoSaveService : IDisposable
{
    bool IsEnabled { get; set; }
    int IntervalSeconds { get; set; }
    void Start(Func<Task> saveAction);
    void Stop();
}
