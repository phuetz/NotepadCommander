using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.Error;

public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        _logger = logger;
    }

    public void HandleError(Exception exception, string context = "")
    {
        _logger.LogError(exception, "Erreur dans {Context}: {Message}", context, exception.Message);
    }

    public void HandleWarning(string message, string context = "")
    {
        _logger.LogWarning("Avertissement dans {Context}: {Message}", context, message);
    }
}
