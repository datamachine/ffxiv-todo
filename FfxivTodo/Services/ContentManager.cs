using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FfxivTodo.Models;
using Newtonsoft.Json;

namespace FfxivTodo.Services;

public sealed class ContentManager
{
    public IReadOnlyList<ContentItem> Items { get; private set; } = [];
    public int DataVersion { get; private set; }

    private readonly Dictionary<uint, ContentItem> _itemMap = new();
    private readonly Dictionary<uint, ProgressEntry> _progress = new();
    private Dictionary<uint, List<uint>> _questIdToParentIds = new();

    public void LoadContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("FfxivTodo.Data.content.json");
        if (stream == null)
            throw new FileNotFoundException("content.json not found as embedded resource");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        Plugin.Log.Information($"Read content.json: {json.Length} bytes");
        try
        {
            var data = JsonConvert.DeserializeObject<ContentDb>(json);
            Plugin.Log.Information($"Deserialization result: data={(data != null ? "non-null" : "NULL")}, items={data?.Items?.Length ?? -1}");
            Items = data?.Items ?? [];
            DataVersion = data?.Version ?? 0;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to deserialize content.json: {ex.Message}");
            Plugin.Log.Error($"Exception type: {ex.GetType().FullName}");
            if (ex.InnerException != null)
                Plugin.Log.Error($"Inner: {ex.InnerException.Message}");
            Items = [];
            DataVersion = 0;
        }

        _itemMap.Clear();
        foreach (var item in Items)
            _itemMap[item.Id] = item;
        BuildQuestToParentIndex();
    }

    public void LoadFromList(IReadOnlyList<ContentItem> items)
    {
        Items = items;
        _itemMap.Clear();
        foreach (var item in Items)
            _itemMap[item.Id] = item;
        BuildQuestToParentIndex();
    }

    public void SetProgress(Dictionary<uint, ProgressEntry> progress)
    {
        _progress.Clear();
        foreach (var kvp in progress)
            _progress[kvp.Key] = kvp.Value;
    }

    public bool IsLocked(uint itemId)
    {
        if (!_itemMap.TryGetValue(itemId, out var item))
            return true;

        return item.PrerequisiteIds.Any(prereqId =>
            !_progress.TryGetValue(prereqId, out var entry) ||
            entry.Status != ItemStatus.Completed);
    }

    public IReadOnlyList<ContentItem> GetPrerequisites(uint itemId)
    {
        if (!_itemMap.TryGetValue(itemId, out var item))
            return [];

        return item.PrerequisiteIds
            .Where(id => _itemMap.ContainsKey(id))
            .Select(id => _itemMap[id])
            .ToList();
    }

    public IReadOnlyList<ContentItem> GetChildren(Expansion expansion, ContentCategory? category = null)
    {
        return Items
            .Where(i => i.Expansion == expansion)
            .Where(i => category == null || i.Category == category)
            .ToList();
    }

    public IReadOnlyList<IGrouping<ContentCategory, ContentItem>> GetGroupedByCategory(Expansion expansion)
    {
        return Items
            .Where(i => i.Expansion == expansion)
            .GroupBy(i => i.Category)
            .ToList();
    }

    public IReadOnlyList<IGrouping<Expansion, ContentItem>> GetGroupedByExpansion()
    {
        return Items
            .GroupBy(i => i.Expansion)
            .OrderBy(g => g.Key)
            .ToList();
    }

    public IReadOnlyList<ContentItem> GetUnlockQuests(uint contentId)
    {
        if (!_itemMap.TryGetValue(contentId, out var item))
            return [];

        var quests = new List<ContentItem>();
        foreach (var questId in item.UnlockQuestIds)
        {
            var quest = Items.FirstOrDefault(i => i.Id == questId || i.QuestId == questId);
            if (quest != null)
                quests.Add(quest);
        }

        return quests;
    }

    public ContentItem? FindParentContent(uint itemId)
    {
        if (!_questIdToParentIds.TryGetValue(itemId, out var parentIds))
            return null;

        foreach (var parentId in parentIds)
        {
            if (_itemMap.TryGetValue(parentId, out var parent))
                return parent;
        }

        return null;
    }

    private void BuildQuestToParentIndex()
    {
        _questIdToParentIds = new Dictionary<uint, List<uint>>();
        foreach (var parent in Items)
        {
            foreach (var questId in parent.UnlockQuestIds)
            {
                var child = Items.FirstOrDefault(i => i.Id == questId || i.QuestId == questId);
                if (child != null)
                {
                    if (!_questIdToParentIds.TryGetValue(child.Id, out var list))
                    {
                        list = new List<uint>();
                        _questIdToParentIds[child.Id] = list;
                    }
                    list.Add(parent.Id);
                }
            }
        }
    }
}

public sealed class ContentDb
{
    public int Version { get; set; }
    public ContentItem[] Items { get; set; } = [];
}