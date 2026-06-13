using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FfxivTodo.Models;

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
        var data = JsonSerializer.Deserialize<Dictionary<uint, ProgressEntry>>(json);
        if (data == null)
            return;

        foreach (var kvp in data)
            _entries[kvp.Key] = kvp.Value;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
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