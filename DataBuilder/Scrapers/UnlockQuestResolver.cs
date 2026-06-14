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
    private HashSet<string> _overriddenNames = new(StringComparer.OrdinalIgnoreCase);

    public UnlockQuestResolver(string overrideFilePath)
    {
        _overrideFilePath = overrideFilePath;
        LoadOverrides();
    }

    public bool IsOverridden(string contentName) => _overriddenNames.Contains(contentName);

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

        _overridesByName = file.Overrides
            .GroupBy(o => o.ContentName)
            .ToDictionary(g => g.Key, g => g.First().QuestIds, StringComparer.OrdinalIgnoreCase);

        _overriddenNames = new HashSet<string>(
            file.Overrides.Select(o => o.ContentName), StringComparer.OrdinalIgnoreCase);
    }
}