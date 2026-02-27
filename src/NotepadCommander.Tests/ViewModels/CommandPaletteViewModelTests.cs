using Xunit;
using NotepadCommander.UI.Models;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.Tests.ViewModels;

public class CommandPaletteViewModelTests
{
    private readonly CommandPaletteViewModel _vm = new();

    private List<CommandPaletteItem> CreateTestCommands()
    {
        return new List<CommandPaletteItem>
        {
            new("Nouveau fichier", "Ctrl+N", () => { }),
            new("Ouvrir fichier", "Ctrl+O", () => { }),
            new("Enregistrer", "Ctrl+S", () => { }),
            new("Rechercher", "Ctrl+F", () => { }),
        };
    }

    [Fact]
    public void Show_SetsVisibleAndClearsQuery()
    {
        _vm.SetCommands(CreateTestCommands());
        _vm.Query = "test";

        _vm.Show();

        Assert.True(_vm.IsVisible);
        Assert.Equal(string.Empty, _vm.Query);
    }

    [Fact]
    public void Show_PopulatesAllItems()
    {
        var commands = CreateTestCommands();
        _vm.SetCommands(commands);

        _vm.Show();

        Assert.Equal(4, _vm.Items.Count);
    }

    [Fact]
    public void Hide_ClearsVisibility()
    {
        _vm.IsVisible = true;

        _vm.Hide();

        Assert.False(_vm.IsVisible);
    }

    [Fact]
    public void QueryChanged_FiltersItemsByLabel()
    {
        _vm.SetCommands(CreateTestCommands());
        _vm.Show();

        _vm.Query = "fichier";

        Assert.Equal(2, _vm.Items.Count);
        Assert.All(_vm.Items, item => Assert.Contains("fichier", item.Label, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void QueryChanged_FiltersItemsByShortcut()
    {
        _vm.SetCommands(CreateTestCommands());
        _vm.Show();

        _vm.Query = "Ctrl+F";

        Assert.Single(_vm.Items);
        Assert.Equal("Rechercher", _vm.Items[0].Label);
    }

    [Fact]
    public void ExecuteItem_HidesAndExecutes()
    {
        var executed = false;
        var item = new CommandPaletteItem("Test", "", () => executed = true);
        _vm.IsVisible = true;

        _vm.ExecuteItem(item);

        Assert.False(_vm.IsVisible);
        Assert.True(executed);
    }

    [Fact]
    public void ExecuteItem_NullDoesNothing()
    {
        _vm.IsVisible = true;

        _vm.ExecuteItem(null);

        Assert.True(_vm.IsVisible);
    }
}
