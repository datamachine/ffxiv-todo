using FfxivTodo.Models;
using FfxivTodo.Windows;

namespace FfxivTodo.Tests.Windows;

public sealed class MainWindowFilterLogicTests
{
    [Fact]
    public void GetExpansionLabel_ReturnsFriendlyName()
    {
        Assert.Equal("A Realm Reborn", MainWindowFilterLogic.GetExpansionLabel(Expansion.ARR));
        Assert.Equal("Shadowbringers", MainWindowFilterLogic.GetExpansionLabel(Expansion.ShB));
    }

    [Fact]
    public void MatchesExpansion_WhenExpansionSetEmpty_ReturnsTrue()
    {
        var selected = new HashSet<Expansion>();

        Assert.True(MainWindowFilterLogic.MatchesExpansion(Expansion.EW, selected));
    }
}
