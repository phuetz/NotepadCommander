using NotepadCommander.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NotepadCommander.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void Default_Settings_AreCorrect()
    {
        var settings = new AppSettings();

        Assert.Equal(14, settings.FontSize);
        Assert.Equal("Cascadia Code", settings.FontFamily);
        Assert.True(settings.ShowLineNumbers);
        Assert.False(settings.WordWrap);
        Assert.Equal(4, settings.TabSize);
        Assert.True(settings.UseSpaces);
        Assert.True(settings.AutoIndent);
        Assert.Equal("Light", settings.Theme);
        Assert.False(settings.AutoSave);
        Assert.Equal(60, settings.AutoSaveIntervalSeconds);
        Assert.Equal(100, settings.ZoomLevel);
        Assert.True(settings.HighlightCurrentLine);
        Assert.False(settings.ShowWhitespace);
        Assert.True(settings.BracketMatching);
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        var logger = new LoggerFactory().CreateLogger<SettingsService>();
        var service = new SettingsService(logger);

        service.Settings.FontSize = 20;
        service.Settings.Theme = "Dark";
        service.Reset();

        Assert.Equal(14, service.Settings.FontSize);
        Assert.Equal("Light", service.Settings.Theme);
    }
}
