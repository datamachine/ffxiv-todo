using System;
using System.Collections.Generic;
using System.Linq;
using FfxivTodo.Models;

namespace FfxivTodo.Windows;

public enum FilterState
{
    NotStarted,
    InProgress,
    Completed,
    Locked,
    Ignored
}

public static class MainWindowFilterLogic
{
    public static string GetExpansionLabel(Expansion expansion) => expansion switch
    {
        Expansion.ARR => "A Realm Reborn",
        Expansion.HW => "Heavensward",
        Expansion.SB => "Stormblood",
        Expansion.ShB => "Shadowbringers",
        Expansion.EW => "Endwalker",
        Expansion.DT => "Dawntrail",
        _ => expansion.ToString()
    };

    public static string GetCategoryLabel(ContentCategory category) => category switch
    {
        ContentCategory.BlueUnlock => "Unlock quests",
        ContentCategory.BeastTribe => "Allied Society Quests",
        ContentCategory.SideQuest => "Side quests",
        ContentCategory.JobQuest => "Job quests",
        ContentCategory.RoleQuest => "Role quests",
        ContentCategory.TrialSeries => "Trial series",
        ContentCategory.RaidSeries => "Raid series",
        ContentCategory.AllianceRaid => "Alliance raids",
        ContentCategory.CustomDelivery => "Custom deliveries",
        ContentCategory.SavageRaid => "Savage raids",
        ContentCategory.UltimateRaid => "Ultimate raids",
        ContentCategory.FieldOperation => "Field operations",
        ContentCategory.VariantDungeon => "Variant dungeons",
        ContentCategory.ChaoticRaid => "Chaotic raids",
        ContentCategory.DeepDungeon => "Deep dungeons",
        ContentCategory.RelicWeapon => "Relic weapons",
        ContentCategory.IslandSanctuary => "Island Sanctuary",
        ContentCategory.IshgardianRestoration => "Ishgardian Restoration",
        ContentCategory.FauxHollows => "Faux Hollows",
        ContentCategory.MaskedCarnivale => "The Masked Carnivale",
        ContentCategory.Dungeon => "Dungeons",
        ContentCategory.PvP => "PvP",
        ContentCategory.GoldSaucer => "The Gold Saucer",
        ContentCategory.TreasureHunt => "Treasure hunts",
        ContentCategory.Chocobo => "Companion chocobo",
        _ => category.ToString()
    };

    public static string GetStateLabel(FilterState state) => state switch
    {
        FilterState.NotStarted => "Not started",
        FilterState.InProgress => "In progress",
        _ => state.ToString()
    };

    public static FilterState GetDisplayState(ProgressEntry entry, bool isLocked)
    {
        if (entry.IsIgnored) return FilterState.Ignored;
        if (isLocked) return FilterState.Locked;

        return entry.Status switch
        {
            ItemStatus.Completed => FilterState.Completed,
            ItemStatus.InProgress => FilterState.InProgress,
            _ => FilterState.NotStarted
        };
    }

    public static bool MatchesExpansion(Expansion expansion, HashSet<Expansion> selected) =>
        selected.Count == 0 || selected.Contains(expansion);

    public static bool MatchesCategory(ContentCategory category, HashSet<ContentCategory> selected) =>
        selected.Count == 0 || selected.Contains(category);

    public static bool MatchesState(FilterState state, HashSet<FilterState> selected) =>
        selected.Count == 0 || selected.Contains(state);

    public static string GetSummary<T>(
        IReadOnlyCollection<T> selected,
        Func<T, string> getLabel,
        string allLabel)
    {
        if (selected.Count == 0)
            return allLabel;

        var labels = selected.Select(getLabel).ToList();
        if (labels.Count <= 2)
            return string.Join(", ", labels);

        return $"{labels.Count} selected";
    }
}