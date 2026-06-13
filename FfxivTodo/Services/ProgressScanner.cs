using System.Collections.Generic;
using System.Threading;
using FfxivTodo.Models;

namespace FfxivTodo.Services;

public sealed class ProgressScanner : System.IDisposable
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

        if (item.QuestId.HasValue)
        {
            if (Plugin.QuestManager.IsQuestComplete(item.QuestId.Value))
                entry.Status = ItemStatus.Completed;
            else
                entry.Status = ItemStatus.NotStarted;
        }

        if (item.AchievementId.HasValue)
        {
            if (Plugin.AchievementManager.IsComplete((int)item.AchievementId.Value))
                entry.Status = ItemStatus.Completed;
        }

        if (item.QuestId.HasValue || item.AchievementId.HasValue)
        {
            entry.IsManual = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer?.Dispose();
    }
}