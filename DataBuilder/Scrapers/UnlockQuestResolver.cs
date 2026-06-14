using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class UnlockQuestResolver
{
    private readonly string _overrideFilePath;
    private Dictionary<string, List<uint>> _overridesByName = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex[] UnlockQuestPatterns =
    [
        new(@"completing the quest (.+?)(?:\.|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"starting the quest (.+?)(?:\.|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"the quest (.+?) must be completed", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"unlocked by completing the quest (.+?)(?:\.|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    ];

    public UnlockQuestResolver(string overrideFilePath)
    {
        _overrideFilePath = overrideFilePath;
        LoadOverrides();
    }

    public bool IsOverridden(string contentName) => _overridesByName.ContainsKey(contentName);

    public void Resolve(List<DetailItem> items)
    {
        foreach (var item in items)
        {
            if (!_overridesByName.TryGetValue(item.Name, out var questIds))
                continue;

            item.UnlockQuestIds = new List<uint>(questIds);
        }
    }

    public static List<string> ExtractUnlockQuestNames(string html)
    {
        var names = new List<string>();
        foreach (var pattern in UnlockQuestPatterns)
        {
            foreach (Match match in pattern.Matches(html))
            {
                var name = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
        }
        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task ResolveWithWikiAsync(List<DetailItem> items, Data.CsvDataProvider csv, HttpClient http)
    {
        Resolve(items);

        var dutyCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RaidSeries", "TrialSeries", "AllianceRaid"
        };

        var unresolved = items
            .Where(i => dutyCategories.Contains(i.Category))
            .Where(i => i.UnlockQuestIds.Count == 0)
            .Where(i => i.QuestId == null || i.QuestId == 0)
            .Where(i => !IsOverridden(i.Name))
            .ToList();

        foreach (var item in unresolved)
        {
            if (string.IsNullOrEmpty(item.WikiUrl))
                continue;

            try
            {
                var html = await http.GetStringAsync(item.WikiUrl);
                var questNames = ExtractUnlockQuestNames(html);

                foreach (var questName in questNames)
                {
                    var questRow = csv.LookupQuest(questName);
                    if (questRow != null)
                        item.UnlockQuestIds.Add((uint)questRow.Id);
                }

                if (item.UnlockQuestIds.Count > 0)
                    Console.WriteLine($"  Wiki: {item.Name} → {string.Join(", ", questNames)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  WARN: Failed to scrape {item.WikiUrl}: {ex.Message}");
            }
        }
    }

    private void LoadOverrides()
    {
        if (!File.Exists(_overrideFilePath))
            return;

        var json = File.ReadAllText(_overrideFilePath);
        var file = JsonSerializer.Deserialize<QuestChainOverridesFile>(json);
        if (file?.Overrides == null || file.Overrides.Count == 0)
            return;

        var dict = new Dictionary<string, List<uint>>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in file.Overrides)
        {
            if (dict.ContainsKey(o.ContentName))
            {
                Console.Error.WriteLine(
                    $"Warning: Duplicate ContentName '{o.ContentName}' in quest chain overrides. Keeping first entry.");
                continue;
            }
            dict[o.ContentName] = o.QuestIds;
        }
        _overridesByName = dict;
    }
}