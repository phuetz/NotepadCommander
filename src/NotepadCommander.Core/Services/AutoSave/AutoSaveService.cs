using Microsoft.Extensions.Logging;

namespace NotepadCommander.Core.Services.AutoSave;

public class AutoSaveService : IAutoSaveService, IDisposable
{
    private readonly ILogger<AutoSaveService> _logger;
    private Timer? _timer;
    private Func<Task>? _saveAction;

    public bool IsEnabled { get; set; }
    public int IntervalSeconds { get; set; } = 60;

    public AutoSaveService(ILogger<AutoSaveService> logger)
    {
        _logger = logger;
    }

    public void Start(Func<Task> saveAction)
    {
        _saveAction = saveAction;
        _timer?.Dispose();

        if (!IsEnabled || IntervalSeconds <= 0) return;

        _timer = new Timer(OnTick, null,
            TimeSpan.FromSeconds(IntervalSeconds),
            TimeSpan.FromSeconds(IntervalSeconds));

        _logger.LogDebug("Auto-sauvegarde demarree (intervalle: {Interval}s)", IntervalSeconds);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async void OnTick(object? state)
    {
        try
        {
            if (_saveAction != null && IsEnabled)
            {
                await _saveAction();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur pendant l'auto-sauvegarde");
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
