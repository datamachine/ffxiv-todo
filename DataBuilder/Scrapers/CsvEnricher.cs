using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataBuilder.Data;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class CsvEnricher
{
    private static readonly string[] ExpansionNames = ["ARR", "HW", "SB", "ShB", "EW", "DT"];

    private readonly CsvDataProvider _csv;
    private readonly Dictionary<string, string> _nameOverrides;
    private readonly Dictionary<string, uint> _achievementOverrides;

    public CsvEnricher(CsvDataProvider csv, string cacheDir)
    {
        _csv = csv;
        _nameOverrides = LoadNameOverrides(cacheDir);
        _achievementOverrides = LoadAchievementOverrides();
    }

    private static Dictionary<string, uint> LoadAchievementOverrides()
    {
        var paths = new[]
        {
            Path.Combine("..", "DataBuilder", "Data", "achievement_overrides.json"),
            Path.Combine("DataBuilder", "Data", "achievement_overrides.json"),
        };

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize<AchievementOverridesFile>(json);
            if (file?.Overrides == null) return new Dictionary<string, uint>();
            return file.Overrides.ToDictionary(o => o.ContentName, o => o.AchievementId);
        }

        return new Dictionary<string, uint>();
    }

    public List<DetailItem> Enrich(List<CategoryItem> categoryItems)
    {
        var results = new List<DetailItem>();
        var notFound = 0;

        foreach (var item in categoryItems)
        {
            var name = _nameOverrides.TryGetValue(item.Name, out var overrideName)
                ? overrideName
                : item.Name;

            var wikiName = overrideName ?? item.Name;

            var questRow = _csv.LookupQuest(name);
            var achievementRow = FindMatchingAchievement(_csv, name);

            if (achievementRow == null && _achievementOverrides.TryGetValue(item.Name, out var overrideAchId))
            {
                var ovRow = _csv.LookupAchievementById((int)overrideAchId);
                if (ovRow != null)
                    achievementRow = ovRow;
            }

            if (questRow == null && achievementRow == null)
            {
                Console.WriteLine($"  CSV not found: {item.Name}");
                notFound++;
                results.Add(new DetailItem
                {
                    Name = item.Name,
                    Category = item.Category,
                    Expansion = item.Expansion,
                    AchievementId = (uint?)achievementRow?.Id,
                    WikiUrl = $"https://ffxiv.consolegameswiki.com/wiki/{wikiName.Replace(" ", "_")}",
                });
                continue;
            }

            var prereqs = new List<string>();
            uint? questId = null;
            uint? level = null;
            uint? locationTerritoryId = null;
            string? territoryName = null;
            string? expansion = null;
            string? edbUrl = null;

            if (questRow != null)
            {
                foreach (var pqId in new[] { questRow.PreviousQuest0, questRow.PreviousQuest1, questRow.PreviousQuest2 })
                {
                    if (pqId > 0)
                    {
                        var pqName = _csv.ResolveQuestName(pqId);
                        if (pqName != null) prereqs.Add(pqName);
                    }
                }

                expansion = questRow.Expansion >= 0 && questRow.Expansion < ExpansionNames.Length
                    ? ExpansionNames[questRow.Expansion]
                    : item.Expansion;

                territoryName = _csv.ResolveTerritoryName(questRow.IssuerLocation);
                questId = (uint?)questRow.Id;
                level = (uint?)questRow.ClassJobLevel;
                locationTerritoryId = (uint?)questRow.IssuerLocation;
                edbUrl = $"https://www.garlandtools.org/db/#quest/{questRow.Id}";
            }

            var detail = new DetailItem
            {
                Name = item.Name,
                Category = item.Category,
                Expansion = expansion ?? item.Expansion,
                Level = level,
                QuestId = questId,
                AchievementId = (uint?)achievementRow?.Id,
                // LocationTerritoryId intentionally null — CSV IssuerLocation is PlaceName, not TerritoryType.
                // Wiki detail scrape (Stage 3) provides proper location data resolved at runtime.
                LocationTerritoryName = territoryName,
                PrerequisiteNames = prereqs,
                WikiUrl = $"https://ffxiv.consolegameswiki.com/wiki/{wikiName.Replace(" ", "_")}",
                EdbUrl = edbUrl,
            };

            results.Add(detail);
        }

        Console.WriteLine($"  CSV: {results.Count} enriched, {notFound} not found");
        return results;
    }

    private static AchievementCsvRow? FindMatchingAchievement(CsvDataProvider csv, string name)
    {
        var row = csv.LookupAchievement(name);
        if (row != null) return row;

        row = csv.LookupAchievement("Complete " + name + ".");
        if (row != null) return row;

        row = csv.LookupAchievement("Clear " + name + ".");
        if (row != null) return row;

        if (name.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = name[4..];
            row = csv.LookupAchievement("Complete the " + rest + ".");
            if (row != null) return row;

            row = csv.LookupAchievement("Clear the " + rest + ".");
            if (row != null) return row;
        }

        return null;
    }

    private static Dictionary<string, string> LoadNameOverrides(string cacheDir)
    {
        var path = Path.Combine(cacheDir, "name_overrides.json");
        if (!File.Exists(path)) return new Dictionary<string, string>();

        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<NameOverridesFile>(json);
        if (file?.Overrides == null) return new Dictionary<string, string>();

        return file.Overrides.ToDictionary(o => o.WikiName, o => o.CsvName);
    }

    private sealed class NameOverridesFile
    {
        [JsonPropertyName("overrides")]
        public List<NameOverride> Overrides { get; set; } = new();
    }

    private sealed class NameOverride
    {
        [JsonPropertyName("wikiName")]
        public string WikiName { get; set; } = string.Empty;

        [JsonPropertyName("csvName")]
        public string CsvName { get; set; } = string.Empty;
    }
}