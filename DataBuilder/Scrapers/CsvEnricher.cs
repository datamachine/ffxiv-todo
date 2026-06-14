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

    public CsvEnricher(CsvDataProvider csv, string cacheDir)
    {
        _csv = csv;
        _nameOverrides = LoadNameOverrides(cacheDir);
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

            var row = _csv.LookupQuest(name);
            if (row == null)
            {
                Console.WriteLine($"  CSV not found: {item.Name}");
                notFound++;
                continue;
            }

            var prereqs = new List<string>();
            foreach (var pqId in new[] { row.PreviousQuest0, row.PreviousQuest1, row.PreviousQuest2 })
            {
                if (pqId > 0)
                {
                    var pqName = _csv.ResolveQuestName(pqId);
                    if (pqName != null) prereqs.Add(pqName);
                }
            }

            var expansion = row.Expansion >= 0 && row.Expansion < ExpansionNames.Length
                ? ExpansionNames[row.Expansion]
                : item.Expansion;

            var territoryName = _csv.ResolveTerritoryName(row.IssuerLocation);

            var wikiName = item.Name.Replace(" ", "_");
            var wikiUrl = $"https://ffxiv.consolegameswiki.com/wiki/{wikiName}";

            var detail = new DetailItem
            {
                Name = item.Name,
                Category = item.Category,
                Expansion = expansion,
                Level = (uint?)row.ClassJobLevel,
                QuestId = (uint?)row.Id,
                LocationTerritoryId = (uint?)row.IssuerLocation,
                LocationTerritoryName = territoryName,
                PrerequisiteNames = prereqs,
                WikiUrl = wikiUrl,
                EdbUrl = $"https://www.garlandtools.org/db/#quest/{row.Id}",
            };

            results.Add(detail);
        }

        Console.WriteLine($"  CSV: {results.Count} enriched, {notFound} not found");
        return results;
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