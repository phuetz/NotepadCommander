using NotepadCommander.Core.Models;
using NotepadCommander.Core.Services;
using NotepadCommander.Core.Services.FileWatcher;
using NotepadCommander.Core.Services.Session;
using Microsoft.Extensions.Logging;

namespace NotepadCommander.UI.ViewModels;

public class SessionManagerViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;
    private readonly IFileWatcherService _fileWatcherService;
    private readonly ILogger<SessionManagerViewModel> _logger;
    private readonly TabManagerViewModel _tabManager;

    public SessionManagerViewModel(
        ISessionService sessionService,
        ISettingsService settingsService,
        IFileService fileService,
        IFileWatcherService fileWatcherService,
        ILogger<SessionManagerViewModel> logger,
        TabManagerViewModel tabManager)
    {
        _sessionService = sessionService;
        _settingsService = settingsService;
        _fileService = fileService;
        _fileWatcherService = fileWatcherService;
        _logger = logger;
        _tabManager = tabManager;
    }

    public void SaveSession()
    {
        var session = new SessionData
        {
            ActiveTabIndex = _tabManager.ActiveTab != null
                ? _tabManager.Tabs.IndexOf(_tabManager.ActiveTab)
                : 0
        };

        foreach (var tab in _tabManager.Tabs)
        {
            session.Tabs.Add(new SessionTab
            {
                FilePath = tab.FilePath,
                UnsavedContent = tab.IsModified ? tab.Content : null,
                CursorLine = tab.CursorLine,
                CursorColumn = tab.CursorColumn
            });
        }

        _sessionService.SaveSession(session);
        _settingsService.Save();
    }

    public async Task RestoreSession()
    {
        var session = _sessionService.LoadSession();
        if (session == null || session.Tabs.Count == 0)
        {
            if (_tabManager.Tabs.Count == 0) _tabManager.NewDocument();
            return;
        }

        _tabManager.Tabs.Clear();

        foreach (var sessionTab in session.Tabs)
        {
            try
            {
                if (sessionTab.FilePath != null && File.Exists(sessionTab.FilePath))
                {
                    var doc = await _fileService.OpenAsync(sessionTab.FilePath);
                    var tab = new DocumentTabViewModel(doc)
                    {
                        FontSize = 14 * _tabManager.ZoomLevel / 100.0
                    };

                    if (sessionTab.UnsavedContent != null)
                        tab.Content = sessionTab.UnsavedContent;

                    tab.CursorLine = sessionTab.CursorLine;
                    tab.CursorColumn = sessionTab.CursorColumn;
                    _tabManager.Tabs.Add(tab);

                    _fileWatcherService.WatchFile(sessionTab.FilePath);
                }
                else if (sessionTab.UnsavedContent != null)
                {
                    var doc = _fileService.CreateNew();
                    var tab = new DocumentTabViewModel(doc)
                    {
                        FontSize = 14 * _tabManager.ZoomLevel / 100.0,
                        Content = sessionTab.UnsavedContent
                    };
                    tab.CursorLine = sessionTab.CursorLine;
                    tab.CursorColumn = sessionTab.CursorColumn;
                    _tabManager.Tabs.Add(tab);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de restaurer l'onglet: {Path}", sessionTab.FilePath);
            }
        }

        if (_tabManager.Tabs.Count == 0)
        {
            _tabManager.NewDocument();
        }
        else
        {
            var idx = Math.Clamp(session.ActiveTabIndex, 0, _tabManager.Tabs.Count - 1);
            _tabManager.ActiveTab = _tabManager.Tabs[idx];
        }
    }
}
