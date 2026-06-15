using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataBuilder.Data;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class UnlockQuestResolver
{
    private readonly string _overrideFilePath;
    private Dictionary<string, List<uint>> _overridesByName = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _explicitChains = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] ExpansionNames = ["ARR", "HW", "SB", "ShB", "EW", "DT"];

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

    public List<DetailItem> ResolveWithChainCreation(List<DetailItem> items, CsvDataProvider? csv)
    {
        Resolve(items);

        var newItems = new List<DetailItem>();

        var existingQuestIds = new HashSet<uint>();
        foreach (var item in items)
        {
            if (item.QuestId.HasValue && item.QuestId.Value > 0)
                existingQuestIds.Add(item.QuestId.Value);
        }

        foreach (var item in items.ToList())
        {
            if (item.UnlockQuestIds.Count == 0)
                continue;

            var isExplicit = _explicitChains.Contains(item.Name);

            if (!isExplicit)
            {
                var fullChain = new List<uint>();
                foreach (var terminalQuestId in item.UnlockQuestIds)
                {
                    if (csv != null)
                    {
                        var walked = WalkPrerequisiteChain(terminalQuestId, csv, existingQuestIds);
                        fullChain.AddRange(walked);
                    }
                    else
                    {
                        if (!existingQuestIds.Contains(terminalQuestId))
                            fullChain.Add(terminalQuestId);
                    }
                }

                if (fullChain.Count > 0 && csv != null)
                {
                    item.UnlockQuestIds = fullChain;
                }
            }

            foreach (var questId in item.UnlockQuestIds)
            {
                if (existingQuestIds.Contains(questId))
                    continue;

                var questRow = csv?.LookupQuestById((int)questId);
                var questName = questRow?.Name ?? $"Quest {questId}";

                var newItem = new DetailItem
                {
                    Name = questName,
                    Category = "BlueUnlock",
                    Expansion = questRow != null && questRow.Expansion >= 0 && questRow.Expansion < ExpansionNames.Length
                        ? ExpansionNames[questRow.Expansion]
                        : item.Expansion,
                    Level = questRow?.ClassJobLevel > 0 ? (uint?)questRow.ClassJobLevel : null,
                    QuestId = questId,
                    WikiUrl = $"https://ffxiv.consolegameswiki.com/wiki/{questName.Replace(' ', '_')}",
                };

                if (questRow != null)
                {
                    foreach (var pqId in new[] { questRow.PreviousQuest0, questRow.PreviousQuest1, questRow.PreviousQuest2 })
                    {
                        if (pqId > 0)
                        {
                            var pqName = csv?.LookupQuestById(pqId)?.Name;
                            if (pqName != null)
                                newItem.PrerequisiteNames.Add(pqName);
                        }
                    }
                }

                newItems.Add(newItem);
                items.Add(newItem);
                existingQuestIds.Add(questId);
            }
        }

        return newItems;
    }

    private static List<uint> WalkPrerequisiteChain(uint terminalQuestId, CsvDataProvider csv, HashSet<uint> existingQuestIds)
    {
        var chain = new List<uint>();
        var visited = new HashSet<uint> { terminalQuestId };
        var stack = new Stack<uint>();
        stack.Push(terminalQuestId);

        while (stack.Count > 0)
        {
            var currentId = stack.Pop();

            if (!existingQuestIds.Contains(currentId))
                chain.Insert(0, currentId);

            var row = csv.LookupQuestById((int)currentId);
            if (row == null)
                continue;

            foreach (var pqId in new[] { row.PreviousQuest0, row.PreviousQuest1, row.PreviousQuest2 })
            {
                if (pqId > 0 && visited.Add((uint)pqId))
                    stack.Push((uint)pqId);
            }
        }

        return chain;
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
            "RaidSeries", "TrialSeries", "AllianceRaid", "RoleQuest",
            "RelicWeapon", "IslandSanctuary", "IshgardianRestoration",
            "FauxHollows", "MaskedCarnivale", "VariantDungeon"
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
            if (o.ExplicitChain)
                _explicitChains.Add(o.ContentName);
        }
        _overridesByName = dict;
    }
}