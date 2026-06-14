using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class UnlockQuestResolver
{
    private readonly string _overrideFilePath;
    private Dictionary<string, List<uint>> _overridesByName = new(StringComparer.OrdinalIgnoreCase);

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