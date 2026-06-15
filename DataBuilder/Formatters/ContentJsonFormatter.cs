using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataBuilder.Models;

namespace DataBuilder.Formatters;

public static class ContentJsonFormatter
{
    private static readonly Dictionary<string, int> ExpansionOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARR"] = 0, ["HW"] = 1, ["SB"] = 2, ["ShB"] = 3, ["EW"] = 4, ["DT"] = 5
    };

    private static readonly Dictionary<string, int> CategoryOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SideQuest"] = 0, ["BlueUnlock"] = 1, ["JobQuest"] = 2, ["RoleQuest"] = 3,
        ["TrialSeries"] = 4, ["RaidSeries"] = 5, ["AllianceRaid"] = 6,
        ["BeastTribe"] = 7, ["CustomDelivery"] = 8, ["SavageRaid"] = 9,
        ["UltimateRaid"] = 10, ["FieldOperation"] = 11, ["VariantDungeon"] = 12,
        ["ChaoticRaid"] = 13, ["DeepDungeon"] = 14,
        ["RelicWeapon"] = 15, ["IslandSanctuary"] = 16,
        ["IshgardianRestoration"] = 17, ["FauxHollows"] = 18,
        ["MaskedCarnivale"] = 19,
        ["Dungeon"] = 20, ["PvP"] = 21, ["GoldSaucer"] = 22,
        ["TreasureHunt"] = 23, ["Chocobo"] = 24
    };

    private static readonly HashSet<string> KnownJunkNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "", " ", "patch", "feature quests", "allied society quests achievements",
        "la noscea sidequests", "side quests by location",
        "side story questlines", "records of unusual endeavors", "seasonal events quests",
        "special quests", "aether current quests", "levequests", "grand company",
        "locations", "glamour and customization", "the hunt",
        "reputation", "vendor", "collectables",
        "collaboration quests",
    };

    private static readonly Dictionary<string, string> BeastTribeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ring of Ash"] = "Amalj'aa",
        ["Little Solace"] = "Sylph",
        ["789th Order Dig"] = "Kobold",
        ["Novv's Nursery"] = "Sahagin",
        ["Ehcatl"] = "Ixal",
        ["Ok' Gundu Nakki"] = "Vanu Vanu",
        ["Loth ast Vath"] = "Vath",
        ["Bahrr Lehs"] = "Moogle",
        ["Tamamizu"] = "Kojin",
        ["Castellum Velodyna"] = "Ananta",
        ["Dhoro Iloh"] = "Namazu",
        ["Lydha Lran"] = "Pixie",
        ["Watts's Anvil"] = "Qitari",
        ["Hopl's Stopple"] = "Dwarf",
        ["Svarna"] = "Arkasodara",
        ["A-4 Research"] = "Omicron",
        ["Hoper's Hold"] = "Loporrit",
        ["Dock Poga"] = "Pelupelu",
        ["Gok Golma"] = "Mamool Ja",
        ["Worlar's Echo"] = "Yok Huy",
    };

    // Filter items whose name matches known non-quest patterns
    private static readonly Regex JunkNamePattern = new(
        @"^patch \d+\.\d+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, uint> ExpansionToLevelCap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARR"] = 50,
        ["HW"] = 60,
        ["SB"] = 70,
        ["ShB"] = 80,
        ["EW"] = 90,
        ["DT"] = 100,
    };

    private static readonly HashSet<string> DutyCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "AllianceRaid", "RaidSeries", "NormalRaid", "Trial", "Dungeon", "GuildOrder", "Guildhest"
    };

    private static readonly Dictionary<string, string> NameCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Savage"] = "SavageRaid",
        ["Ultimate"] = "UltimateRaid",
        ["Castrum Lacus Litore"] = "FieldOperation",
        ["Delubrum Reginae"] = "FieldOperation",
        ["The Dalriada"] = "FieldOperation",
        ["The Baldesion Arsenal"] = "FieldOperation",
        ["The Forked Tower"] = "VariantDungeon",
        ["The Cloud of Darkness (Chaotic)"] = "ChaoticRaid",
    };

    private static readonly Dictionary<uint, string> LevelToExpansion = new()
    {
        [50] = "ARR",
        [51] = "HW", [52] = "HW", [53] = "HW", [54] = "HW", [55] = "HW",
        [56] = "HW", [57] = "HW", [58] = "HW", [59] = "HW", [60] = "HW",
        [61] = "SB", [62] = "SB", [63] = "SB", [64] = "SB", [65] = "SB",
        [66] = "SB", [67] = "SB", [68] = "SB", [69] = "SB", [70] = "SB",
        [71] = "ShB", [72] = "ShB", [73] = "ShB", [74] = "ShB", [75] = "ShB",
        [76] = "ShB", [77] = "ShB", [78] = "ShB", [79] = "ShB", [80] = "ShB",
        [81] = "EW", [82] = "EW", [83] = "EW", [84] = "EW", [85] = "EW",
        [86] = "EW", [87] = "EW", [88] = "EW", [89] = "EW", [90] = "EW",
        [91] = "DT", [92] = "DT", [93] = "DT", [94] = "DT", [95] = "DT",
        [96] = "DT", [97] = "DT", [98] = "DT", [99] = "DT", [100] = "DT",
    };

    private static string InferExpansion(uint? level, string category, string currentExpansion, bool hasQuestId)
    {
        if (hasQuestId)
            return currentExpansion;

        if (level.HasValue && level.Value > 0
            && LevelToExpansion.TryGetValue(level.Value, out var exp)
            && category is "BlueUnlock" or "SideQuest")
        {
            return exp;
        }
        return currentExpansion;
    }

    public static FormattedItemsFile Format(List<DetailItem> items)
    {
        // Assign categories to items with empty category based on name patterns
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Category))
                continue;

            foreach (var (pattern, category) in NameCategoryMap)
            {
                if (item.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    item.Category = category;
                    break;
                }
            }
        }

        var validItems = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Where(i => !KnownJunkNames.Contains(i.Name.Trim()))
            .Where(i => !JunkNamePattern.IsMatch(i.Name.Trim()))
            .Where(i => !string.IsNullOrWhiteSpace(i.Category))
            .ToList();

        // Refine expansions based on level where available
        foreach (var item in validItems)
        {
            item.Expansion = InferExpansion(item.Level, item.Category, item.Expansion, item.QuestId.HasValue);
        }

        // Infer level from expansion for duty categories where all items use the expansion cap
        foreach (var item in validItems)
        {
            if ((item.Level == null || item.Level == 0)
                && DutyCategories.Contains(item.Category)
                && ExpansionToLevelCap.TryGetValue(item.Expansion, out var cap))
            {
                item.Level = cap;
            }
        }

        // Rename BeastTribe items to use society names
        foreach (var item in validItems)
        {
            if (item.Category == "BeastTribe" && BeastTribeNames.TryGetValue(item.Name, out var societyName))
                item.Name = societyName;
        }

        var sorted = validItems
            .OrderBy(i => ExpansionOrder.GetValueOrDefault(i.Expansion, 99))
            .ThenBy(i => CategoryOrder.GetValueOrDefault(i.Category, 99))
            .ThenBy(i => i.Level ?? 0)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var formattedItems = new List<FormattedItem>();
        uint nextId = 1;

        foreach (var item in sorted)
        {
            var formatted = new FormattedItem
            {
                Id = nextId,
                Name = item.Name,
                Level = item.Level ?? 0,
                Expansion = item.Expansion,
                Category = item.Category,
                LocationTerritoryName = item.LocationTerritoryName,
                LocationTerritoryId = item.LocationTerritoryId,
                LocationMapX = item.LocationMapX,
                LocationMapY = item.LocationMapY,
                QuestId = item.QuestId,
                AchievementId = item.AchievementId,
                UnlockQuestIds = item.UnlockQuestIds,
                WikiUrl = item.WikiUrl,
            };

            nameToId[item.Name] = nextId;
            formattedItems.Add(formatted);
            nextId++;
        }

        for (var i = 0; i < sorted.Count; i++)
        {
            foreach (var prereqName in sorted[i].PrerequisiteNames)
            {
                if (nameToId.TryGetValue(prereqName, out var prereqId))
                    formattedItems[i].PrerequisiteIds.Add(prereqId);
                else
                    Console.Error.WriteLine($"WARN: Unresolvable prerequisite '{prereqName}' for '{sorted[i].Name}'");
            }
        }

        return new FormattedItemsFile { Version = 1, Items = formattedItems };
    }
}