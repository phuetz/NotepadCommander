using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadCommander.UI.Views.Components;
using Avalonia;

namespace NotepadCommander.UI.ViewModels;

public partial class ToolbarViewModel : ViewModelBase
{
    private readonly MainWindowViewModel? _mainViewModel;
    private readonly Func<ToolbarTab, object?> _toolbarFactory;
    private readonly Dictionary<ToolbarTab, object?> _toolbarContentCache = new();

    public enum ToolbarTab
    {
        Home,
        Edit,
        View,
        Tools,
        Help
    }

    public enum BackstageSection
    {
        Home,
        Open,
        Save,
        Info
    }

    [ObservableProperty]
    private ToolbarTab activeTab = ToolbarTab.Home;

    [ObservableProperty]
    private bool isFileBackstageOpen;

    [ObservableProperty]
    private BackstageSection selectedBackstageSection = BackstageSection.Home;

    public ToolbarViewModel(
        MainWindowViewModel? mainViewModel = null,
        Func<ToolbarTab, object?>? toolbarFactory = null)
    {
        _mainViewModel = mainViewModel;
        _toolbarFactory = toolbarFactory ?? CreateToolbarForTab;
    }

    public object? ToolbarContent
    {
        get
        {
            if (_toolbarContentCache.TryGetValue(ActiveTab, out var cachedContent))
                return cachedContent;

            var toolbar = _toolbarFactory(ActiveTab);

            if (toolbar is StyledElement styledElement && _mainViewModel != null)
                styledElement.DataContext = _mainViewModel;

            _toolbarContentCache[ActiveTab] = toolbar;
            return toolbar;
        }
    }

    private static object? CreateToolbarForTab(ToolbarTab tab)
    {
        return tab switch
        {
            ToolbarTab.Home => new HomeToolbar(),
            ToolbarTab.Edit => new EditToolbar(),
            ToolbarTab.View => new ViewToolbar(),
            ToolbarTab.Tools => new ToolsToolbar(),
            ToolbarTab.Help => new HelpToolbar(),
            _ => null
        };
    }

    public bool IsHomeTabActive => ActiveTab == ToolbarTab.Home;
    public bool IsEditTabActive => ActiveTab == ToolbarTab.Edit;
    public bool IsViewTabActive => ActiveTab == ToolbarTab.View;
    public bool IsToolsTabActive => ActiveTab == ToolbarTab.Tools;
    public bool IsHelpTabActive => ActiveTab == ToolbarTab.Help;
    public bool IsBackstageHomeSelected => SelectedBackstageSection == BackstageSection.Home;
    public bool IsBackstageOpenSelected => SelectedBackstageSection == BackstageSection.Open;
    public bool IsBackstageSaveSelected => SelectedBackstageSection == BackstageSection.Save;
    public bool IsBackstageInfoSelected => SelectedBackstageSection == BackstageSection.Info;

    partial void OnActiveTabChanged(ToolbarTab value)
    {
        IsFileBackstageOpen = false;
        OnPropertyChanged(nameof(ToolbarContent));
        OnPropertyChanged(nameof(IsHomeTabActive));
        OnPropertyChanged(nameof(IsEditTabActive));
        OnPropertyChanged(nameof(IsViewTabActive));
        OnPropertyChanged(nameof(IsToolsTabActive));
        OnPropertyChanged(nameof(IsHelpTabActive));
    }

    partial void OnSelectedBackstageSectionChanged(BackstageSection value)
    {
        OnPropertyChanged(nameof(IsBackstageHomeSelected));
        OnPropertyChanged(nameof(IsBackstageOpenSelected));
        OnPropertyChanged(nameof(IsBackstageSaveSelected));
        OnPropertyChanged(nameof(IsBackstageInfoSelected));
    }

    [RelayCommand]
    private void SelectHomeTab() { IsFileBackstageOpen = false; ActiveTab = ToolbarTab.Home; }

    [RelayCommand]
    private void SelectEditTab() { IsFileBackstageOpen = false; ActiveTab = ToolbarTab.Edit; }

    [RelayCommand]
    private void SelectViewTab() { IsFileBackstageOpen = false; ActiveTab = ToolbarTab.View; }

    [RelayCommand]
    private void SelectToolsTab() { IsFileBackstageOpen = false; ActiveTab = ToolbarTab.Tools; }

    [RelayCommand]
    private void SelectHelpTab() { IsFileBackstageOpen = false; ActiveTab = ToolbarTab.Help; }

    [RelayCommand]
    private void ToggleFileBackstage()
    {
        var nextState = !IsFileBackstageOpen;
        IsFileBackstageOpen = nextState;
        if (nextState) SelectedBackstageSection = BackstageSection.Home;
    }

    [RelayCommand]
    private void CloseFileBackstage() => IsFileBackstageOpen = false;

    [RelayCommand]
    private void SelectBackstageHome() { IsFileBackstageOpen = true; SelectedBackstageSection = BackstageSection.Home; }

    [RelayCommand]
    private void SelectBackstageOpen() { IsFileBackstageOpen = true; SelectedBackstageSection = BackstageSection.Open; }

    [RelayCommand]
    private void SelectBackstageSave() { IsFileBackstageOpen = true; SelectedBackstageSection = BackstageSection.Save; }

    [RelayCommand]
    private void SelectBackstageInfo() { IsFileBackstageOpen = true; SelectedBackstageSection = BackstageSection.Info; }

    public void SelectTabByNumber(int tabNumber)
    {
        IsFileBackstageOpen = false;
        ActiveTab = tabNumber switch
        {
            1 => ToolbarTab.Home,
            2 => ToolbarTab.Edit,
            3 => ToolbarTab.View,
            4 => ToolbarTab.Tools,
            5 => ToolbarTab.Help,
            _ => ActiveTab
        };
    }
}
