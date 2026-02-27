using Xunit;
using NotepadCommander.Core.Services;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.Tests.ViewModels;

public class EditorSettingsViewModelTests
{
    private class FakeSettingsService : ISettingsService
    {
        public AppSettings Settings { get; } = new();
        public void Load() { }
        public void Save() { }
        public void Reset() { Settings.ShowLineNumbers = true; Settings.WordWrap = false; Settings.Theme = "Light"; }
    }

    private readonly FakeSettingsService _settings = new();

    private EditorSettingsViewModel CreateVm() => new(_settings);

    [Fact]
    public void Constructor_LoadsFromSettings()
    {
        _settings.Settings.ShowLineNumbers = false;
        _settings.Settings.WordWrap = true;
        _settings.Settings.Theme = "Dark";
        _settings.Settings.HighlightCurrentLine = false;
        _settings.Settings.ZoomLevel = 150;

        var vm = CreateVm();

        Assert.False(vm.ShowLineNumbers);
        Assert.True(vm.WordWrap);
        Assert.Equal("Dark", vm.CurrentTheme);
        Assert.False(vm.HighlightCurrentLine);
        Assert.Equal(150, vm.ZoomLevel);
    }

    [Fact]
    public void ToggleWordWrap_TogglesAndSyncsSettings()
    {
        var vm = CreateVm();
        var initial = vm.WordWrap;

        vm.ToggleWordWrap();

        Assert.Equal(!initial, vm.WordWrap);
        Assert.Equal(vm.WordWrap, _settings.Settings.WordWrap);
    }

    [Fact]
    public void ToggleLineNumbers_TogglesAndSyncsSettings()
    {
        var vm = CreateVm();
        var initial = vm.ShowLineNumbers;

        vm.ToggleLineNumbers();

        Assert.Equal(!initial, vm.ShowLineNumbers);
        Assert.Equal(vm.ShowLineNumbers, _settings.Settings.ShowLineNumbers);
    }

    [Fact]
    public void SetThemeLight_SetsLightTheme()
    {
        var vm = CreateVm();

        vm.SetThemeDark();
        vm.SetThemeLight();

        Assert.Equal("Light", vm.CurrentTheme);
        Assert.Equal("Light", _settings.Settings.Theme);
    }

    [Fact]
    public void SetThemeDark_SetsDarkTheme()
    {
        var vm = CreateVm();

        vm.SetThemeDark();

        Assert.Equal("Dark", vm.CurrentTheme);
        Assert.Equal("Dark", _settings.Settings.Theme);
    }

    [Fact]
    public void ZoomIn_IncreasesBy10_CapsAt300()
    {
        var vm = CreateVm();
        vm.ZoomReset();
        Assert.Equal(100, vm.ZoomLevel);

        vm.ZoomIn();
        Assert.Equal(110, vm.ZoomLevel);

        // Cap at 300
        for (int i = 0; i < 30; i++) vm.ZoomIn();
        Assert.Equal(300, vm.ZoomLevel);
    }

    [Fact]
    public void ZoomOut_DecreasesBy10_FloorsAt50()
    {
        var vm = CreateVm();
        vm.ZoomReset();

        vm.ZoomOut();
        Assert.Equal(90, vm.ZoomLevel);

        // Floor at 50
        for (int i = 0; i < 20; i++) vm.ZoomOut();
        Assert.Equal(50, vm.ZoomLevel);
    }

    [Fact]
    public void ZoomReset_SetsTo100()
    {
        var vm = CreateVm();
        vm.ZoomIn();
        vm.ZoomIn();

        vm.ZoomReset();

        Assert.Equal(100, vm.ZoomLevel);
    }

    [Fact]
    public void ToggleFullScreen_Toggles()
    {
        var vm = CreateVm();
        Assert.False(vm.IsFullScreen);

        vm.ToggleFullScreen();
        Assert.True(vm.IsFullScreen);

        vm.ToggleFullScreen();
        Assert.False(vm.IsFullScreen);
    }

    [Fact]
    public void ToggleMinimap_TogglesAndSyncsSettings()
    {
        var vm = CreateVm();

        vm.ToggleMinimap();

        Assert.True(vm.ShowMinimap);
        Assert.True(_settings.Settings.ShowMinimap);
    }

    [Fact]
    public void ZoomLevelChanged_SyncsToSettings()
    {
        var vm = CreateVm();

        vm.ZoomIn(); // triggers OnZoomLevelChanged

        Assert.Equal(vm.ZoomLevel, _settings.Settings.ZoomLevel);
    }

    [Fact]
    public void ToggleSidePanel_Toggles()
    {
        var vm = CreateVm();
        Assert.False(vm.IsSidePanelVisible);

        vm.ToggleSidePanel();
        Assert.True(vm.IsSidePanelVisible);

        vm.ToggleSidePanel();
        Assert.False(vm.IsSidePanelVisible);
    }
}
