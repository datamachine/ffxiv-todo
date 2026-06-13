using System;
using System.Collections.Generic;
using System.Threading;
using FfxivTodo.Models;
using Lumina.Excel.Sheets;

namespace FfxivTodo.Services;

public sealed class ProgressScanner : IDisposable
{
    private readonly ProgressStore _store;
    private Timer? _debounceTimer;
    private int _debounceMs;
    private uint _lastTerritoryId;
    private bool _disposed;

    public ProgressScanner(ProgressStore store)
    {
        _store = store;
    }

    public void SetDebounce(int milliseconds)
    {
        _debounceMs = milliseconds;
    }

    public void ScanAll(IReadOnlyList<ContentItem> items)
    {
        foreach (var item in items)
            ScanItem(item);
        _store.Save();
    }

    public void ScanZone(IReadOnlyList<ContentItem> items)
    {
        var territoryId = Plugin.ClientState.TerritoryType;
        if (territoryId == _lastTerritoryId)
            return;
        _lastTerritoryId = territoryId;

        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            ScanAll(items);
        }, null, _debounceMs, Timeout.Infinite);
    }

    private void ScanItem(ContentItem item)
    {
        var entry = _store.GetOrCreate(item.Id);
        if (entry.IsManual)
            return;

        var hasQuestCheck = item.QuestId.HasValue;
        var hasAchievementCheck = item.AchievementId.HasValue;

        if (!hasQuestCheck && !hasAchievementCheck)
            return;

        if (hasQuestCheck)
        {
            if (QuestHelper.IsQuestComplete(item.QuestId!.Value))
                entry.Status = ItemStatus.Completed;
            else
                entry.Status = ItemStatus.NotStarted;
        }

        if (hasAchievementCheck)
        {
            if (IsAchievementComplete(item.AchievementId!.Value))
                entry.Status = ItemStatus.Completed;
            else
                entry.Status = ItemStatus.NotStarted;
        }
    }

    private static bool IsAchievementComplete(uint achievementId)
    {
        if (!Plugin.UnlockState.IsAchievementListLoaded)
            return false;
        var sheet = Plugin.DataManager.GameData.GetExcelSheet<Achievement>();
        if (sheet == null)
            return false;
        if (!sheet.TryGetRow(achievementId, out var row))
            return false;
        return Plugin.UnlockState.IsAchievementComplete(row);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer?.Dispose();
    }
}
