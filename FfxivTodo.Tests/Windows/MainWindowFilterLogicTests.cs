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

    [Fact]
    public void GetCategoryLabel_MapsBlueUnlockToUnlockQuests()
    {
        Assert.Equal("Unlock quests", MainWindowFilterLogic.GetCategoryLabel(ContentCategory.BlueUnlock));
    }

    [Fact]
    public void GetSummary_WhenTwoSelections_ReturnsCommaSeparatedLabels()
    {
        var selected = new HashSet<Expansion> { Expansion.EW, Expansion.DT };

        Assert.Equal(
            "Endwalker, Dawntrail",
            MainWindowFilterLogic.GetSummary(
                selected,
                MainWindowFilterLogic.GetExpansionLabel,
                allLabel: "All"));
    }

    [Fact]
    public void GetDisplayState_IgnoredTakesPrecedenceOverLocked()
    {
        var entry = new ProgressEntry { Status = ItemStatus.NotStarted, IsIgnored = true };

        Assert.Equal(
            FilterState.Ignored,
            MainWindowFilterLogic.GetDisplayState(entry, isLocked: true));
    }

    [Fact]
    public void GetDisplayState_LockedDoesNotMapToNotStarted()
    {
        var entry = new ProgressEntry { Status = ItemStatus.NotStarted, IsIgnored = false };

        Assert.Equal(
            FilterState.Locked,
            MainWindowFilterLogic.GetDisplayState(entry, isLocked: true));
    }

    [Fact]
    public void MatchesStates_WhenNoStatesSelected_ReturnsTrue()
    {
        var selected = new HashSet<FilterState>();

        Assert.True(MainWindowFilterLogic.MatchesState(FilterState.Completed, selected));
    }

    [Fact]
    public void MatchesStates_WhenSelected_UsesOrSemantics()
    {
        var selected = new HashSet<FilterState> { FilterState.Completed, FilterState.Locked };

        Assert.True(MainWindowFilterLogic.MatchesState(FilterState.Locked, selected));
        Assert.False(MainWindowFilterLogic.MatchesState(FilterState.InProgress, selected));
    }

    [Fact]
    public void GetSummary_WhenThreeSelections_ReturnsCountSummary()
    {
        var selected = new HashSet<ContentCategory>
        {
            ContentCategory.SideQuest,
            ContentCategory.BlueUnlock,
            ContentCategory.JobQuest
        };

        Assert.Equal(
            "3 selected",
            MainWindowFilterLogic.GetSummary(
                selected,
                MainWindowFilterLogic.GetCategoryLabel,
                allLabel: "All"));
    }
}
