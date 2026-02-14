namespace NotepadCommander.Core.Services.Error;

public interface IErrorHandler
{
    void HandleError(Exception exception, string context = "");
    void HandleWarning(string message, string context = "");
}
