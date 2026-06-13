using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FfxivTodo.Models;

namespace FfxivTodo.Services;

public sealed class ContentManager
{
    public IReadOnlyList<ContentItem> Items { get; private set; } = [];
    public int DataVersion { get; private set; }

    private readonly Dictionary<uint, ContentItem> _itemMap = new();
    private readonly Dictionary<uint, ProgressEntry> _progress = new();

    public void LoadContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("FfxivTodo.Data.content.json");
        if (stream == null)
            throw new FileNotFoundException("content.json not found as embedded resource");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var data = JsonSerializer.Deserialize<ContentDb>(json);
        Items = data?.Items ?? [];
        DataVersion = data?.Version ?? 0;

        _itemMap.Clear();
        foreach (var item in Items)
            _itemMap[item.Id] = item;
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

    private sealed class ContentDb
    {
        public int Version { get; set; }
        public ContentItem[] Items { get; set; } = [];
    }
}