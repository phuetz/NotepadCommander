namespace NotepadCommander.Core.Services.Terminal;

public interface ITerminalService : IDisposable
{
    event Action<string>? OutputReceived;
    event Action? ProcessExited;
    void Start(string workingDirectory);
    void SendInput(string input);
    void Stop();
    bool IsRunning { get; }
}
