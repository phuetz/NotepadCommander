using Xunit;
using NotepadCommander.Core.Services.Session;

namespace NotepadCommander.Tests;

public class SessionServiceTests
{
    [Fact]
    public void SessionData_DefaultValues()
    {
        var session = new SessionData();
        Assert.Empty(session.Tabs);
        Assert.Equal(0, session.ActiveTabIndex);
        Assert.Equal(1280, session.WindowWidth);
        Assert.Equal(800, session.WindowHeight);
    }

    [Fact]
    public void SessionTab_DefaultValues()
    {
        var tab = new SessionTab();
        Assert.Null(tab.FilePath);
        Assert.Null(tab.UnsavedContent);
        Assert.Equal(1, tab.CursorLine);
        Assert.Equal(1, tab.CursorColumn);
    }

    [Fact]
    public void SessionData_RoundTrip()
    {
        var session = new SessionData
        {
            ActiveTabIndex = 2,
            WindowWidth = 1920,
            WindowHeight = 1080,
            Tabs = new List<SessionTab>
            {
                new() { FilePath = @"C:\test.txt", CursorLine = 10, CursorColumn = 5 },
                new() { UnsavedContent = "unsaved content" }
            }
        };

        Assert.Equal(2, session.Tabs.Count);
        Assert.Equal(@"C:\test.txt", session.Tabs[0].FilePath);
        Assert.Equal(10, session.Tabs[0].CursorLine);
        Assert.Equal("unsaved content", session.Tabs[1].UnsavedContent);
    }
}
