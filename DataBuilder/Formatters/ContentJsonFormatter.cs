using System;
using System.Collections.Generic;
using System.Linq;
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
        ["BeastTribe"] = 7, ["CustomDelivery"] = 8
    };

    public static FormattedItemsFile Format(List<DetailItem> items)
    {
        var validItems = items.Where(i =>
            !string.IsNullOrWhiteSpace(i.Name) &&
            !string.IsNullOrWhiteSpace(i.Category) &&
            !string.IsNullOrWhiteSpace(i.Expansion)).ToList();

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
                LocationTerritoryId = item.LocationTerritoryId,
                LocationMapX = item.LocationMapX,
                LocationMapY = item.LocationMapY,
                QuestId = item.QuestId,
                AchievementId = item.AchievementId,
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