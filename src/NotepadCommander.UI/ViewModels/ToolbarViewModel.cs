using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NotepadCommander.UI.ViewModels;

public partial class ToolbarViewModel : ViewModelBase
{
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

    public ToolbarViewModel(ShellViewModel? mainViewModel = null)
    {
        // Kept for test compatibility; no longer creates Views
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
