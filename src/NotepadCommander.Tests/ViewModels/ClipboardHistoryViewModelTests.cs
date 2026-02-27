using Xunit;
using NotepadCommander.UI.ViewModels;

namespace NotepadCommander.Tests.ViewModels;

public class ClipboardHistoryViewModelTests
{
    private readonly ClipboardHistoryViewModel _vm = new();

    [Fact]
    public void AddToHistory_AddsItem()
    {
        _vm.AddToHistory("hello");

        Assert.Single(_vm.History);
        Assert.Equal("hello", _vm.History[0]);
    }

    [Fact]
    public void AddToHistory_IgnoresNullOrEmpty()
    {
        _vm.AddToHistory(null!);
        _vm.AddToHistory("");

        Assert.Empty(_vm.History);
    }

    [Fact]
    public void AddToHistory_MoveDuplicateToTop()
    {
        _vm.AddToHistory("first");
        _vm.AddToHistory("second");
        _vm.AddToHistory("first");

        Assert.Equal(2, _vm.History.Count);
        Assert.Equal("first", _vm.History[0]);
        Assert.Equal("second", _vm.History[1]);
    }

    [Fact]
    public void AddToHistory_EnforcesMax20()
    {
        for (int i = 0; i < 25; i++)
            _vm.AddToHistory($"item{i}");

        Assert.Equal(20, _vm.History.Count);
        Assert.Equal("item24", _vm.History[0]);
    }

    [Fact]
    public void Show_SetsIsVisible()
    {
        _vm.AddToHistory("test");
        _vm.Show();

        Assert.True(_vm.IsVisible);
        Assert.NotEmpty(_vm.FilteredItems);
    }

    [Fact]
    public void Hide_ClearsIsVisible()
    {
        _vm.AddToHistory("test");
        _vm.Show();
        _vm.Hide();

        Assert.False(_vm.IsVisible);
    }

    [Fact]
    public void Filter_NarrowsItems()
    {
        _vm.AddToHistory("hello world");
        _vm.AddToHistory("goodbye");
        _vm.Show();

        _vm.Filter = "hello";

        Assert.Single(_vm.FilteredItems);
        Assert.Equal("hello world", _vm.FilteredItems[0]);
    }

    [Fact]
    public void MoveDown_WrapsAround()
    {
        _vm.AddToHistory("a");
        _vm.AddToHistory("b");
        _vm.Show();

        _vm.MoveDown();
        Assert.Equal(1, _vm.SelectedIndex);

        _vm.MoveDown();
        Assert.Equal(0, _vm.SelectedIndex);
    }

    [Fact]
    public void SelectCurrent_FiresPasteRequested()
    {
        string? pasted = null;
        _vm.PasteRequested += text => pasted = text;
        _vm.AddToHistory("test-clip");
        _vm.Show();

        _vm.SelectCurrent();

        Assert.Equal("test-clip", pasted);
        Assert.False(_vm.IsVisible);
    }
}
