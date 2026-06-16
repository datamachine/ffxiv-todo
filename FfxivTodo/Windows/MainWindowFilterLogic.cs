using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FfxivTodo.Models;

namespace FfxivTodo.Windows;

public enum FilterState
{
    NotStarted,
    InProgress,
    Completed,
    Unlocked,
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
        FilterState.Unlocked => "Unlocked",
        _ => state.ToString()
    };

    public static string GetStatusLabel(ItemStatus status) => status switch
    {
        ItemStatus.NotStarted => "Not started",
        ItemStatus.InProgress => "In progress",
        ItemStatus.Unlocked => "Unlocked",
        ItemStatus.Completed => "Completed",
        _ => status.ToString()
    };

    public static FilterState GetDisplayState(ProgressEntry entry, bool isLocked)
    {
        if (entry.IsIgnored) return FilterState.Ignored;
        if (isLocked) return FilterState.Locked;

        return entry.Status switch
        {
            ItemStatus.Completed => FilterState.Completed,
            ItemStatus.Unlocked => FilterState.Unlocked,
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

    public static string GetStatusIcon(ItemStatus status, bool locked)
    {
        if (locked) return "\u2717";
        return status switch
        {
            ItemStatus.Completed => "\u2713",
            ItemStatus.Unlocked => "\u25C9",
            ItemStatus.InProgress => "\u25D0",
            _ => "\u25CB"
        };
    }

    public static string GetStatusIcon(ProgressEntry entry, bool locked) =>
        GetStatusIcon(entry.Status, locked);

    public static Vector4 GetStatusColor(ItemStatus status, bool locked, bool isManual = false)
    {
        if (isManual) return new Vector4(0.7f, 0.7f, 1.0f, 1);
        if (locked) return new Vector4(0.4f, 0.4f, 0.4f, 1);
        return status switch
        {
            ItemStatus.Completed => new Vector4(0.4f, 0.85f, 0.4f, 1),
            ItemStatus.Unlocked => new Vector4(0.3f, 0.8f, 1.0f, 1),
            ItemStatus.InProgress => new Vector4(0.9f, 0.8f, 0.25f, 1),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1)
        };
    }

    public static Vector4 GetFilterStateColor(FilterState state) => state switch
    {
        FilterState.Completed => new Vector4(0.25f, 0.45f, 0.25f, 1f),
        FilterState.Unlocked => new Vector4(0.2f, 0.35f, 0.55f, 1f),
        FilterState.InProgress => new Vector4(0.55f, 0.45f, 0.15f, 1f),
        FilterState.Locked => new Vector4(0.5f, 0.2f, 0.2f, 1f),
        FilterState.Ignored => new Vector4(0.35f, 0.2f, 0.35f, 1f),
        FilterState.NotStarted => new Vector4(0.6f, 0.6f, 0.6f, 1f),
        _ => new Vector4(0.4f, 0.4f, 0.4f, 1f)
    };
}