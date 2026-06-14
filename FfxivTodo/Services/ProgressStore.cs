using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FfxivTodo.Models;
using Newtonsoft.Json;

namespace FfxivTodo.Services;

public sealed class ProgressStore
{
    private readonly string _filePath;
    private readonly Dictionary<uint, ProgressEntry> _entries = new();

    public ProgressStore(string configDirectory)
    {
        _filePath = Path.Combine(configDirectory, "progress.json");
    }

    public void Load()
    {
        _entries.Clear();
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<uint, ProgressEntry>>(json);
        if (data == null)
            return;

        foreach (var kvp in data)
            _entries[kvp.Key] = kvp.Value;
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
        File.WriteAllText(_filePath, json);
    }

    public ProgressEntry GetOrCreate(uint itemId)
    {
        if (!_entries.TryGetValue(itemId, out var entry))
        {
            entry = new ProgressEntry();
            _entries[itemId] = entry;
        }
        return entry;
    }

    public Dictionary<uint, ProgressEntry> GetAll()
    {
        return new Dictionary<uint, ProgressEntry>(_entries);
    }

    public void SetStatus(uint itemId, ItemStatus status, bool isManual)
    {
        var entry = GetOrCreate(itemId);
        entry.Status = status;
        entry.IsManual = isManual;
    }

    public void SetStatus(uint itemId, ItemStatus status, bool isManual, IReadOnlyList<ContentItem> allItems)
    {
        var entry = GetOrCreate(itemId);
        entry.Status = status;
        entry.IsManual = isManual;

        if (status != ItemStatus.Completed)
            return;

        var questItem = allItems.FirstOrDefault(i => i.Id == itemId);
        if (questItem?.QuestId == null)
            return;

        foreach (var parent in allItems.Where(i => i.UnlockQuestIds.Length > 0))
        {
            if (parent.UnlockQuestIds[^1] != questItem.QuestId.Value)
                continue;

            var allChainQuestsCompleted = parent.UnlockQuestIds.All(qid =>
            {
                var chainQuest = allItems.FirstOrDefault(i => i.QuestId == qid);
                if (chainQuest == null) return false;
                var chainEntry = GetOrCreate(chainQuest.Id);
                return chainEntry.Status == ItemStatus.Completed;
            });

            if (!allChainQuestsCompleted)
                continue;

            var parentEntry = GetOrCreate(parent.Id);
            if (!parentEntry.IsManual)
                parentEntry.Status = ItemStatus.Completed;
        }
    }

    public void SetTracked(uint itemId, bool isTracked)
    {
        GetOrCreate(itemId).IsTracked = isTracked;
    }

    public void SetIgnored(uint itemId, bool isIgnored)
    {
        GetOrCreate(itemId).IsIgnored = isIgnored;
    }

    public void ClearManualFlag(uint itemId)
    {
        if (_entries.TryGetValue(itemId, out var entry))
            entry.IsManual = false;
    }
}
