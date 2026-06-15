using System;
using System.Collections.Generic;
using System.Linq;
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
        AutoCompleteParents(items);
        _store.Save();
    }

    private void AutoCompleteParents(IReadOnlyList<ContentItem> items)
    {
        var questIdToItem = new Dictionary<uint, ContentItem>();
        foreach (var item in items)
        {
            if (item.QuestId.HasValue)
                questIdToItem[item.QuestId.Value] = item;
        }

        foreach (var parent in items)
        {
            if (parent.UnlockQuestIds.Length == 0)
                continue;

            var parentEntry = _store.GetOrCreate(parent.Id);
            if (parentEntry.IsManual)
                continue;

            // If parent has an achievement override and the achievement is complete,
            // that is the definitive completion signal — skip quest chain evaluation
            if (parent.AchievementId.HasValue && Plugin.UnlockState.IsAchievementListLoaded)
            {
                var achId = parent.AchievementId.Value;
                var achComplete = IsAchievementComplete(achId);
                Plugin.Log.Debug($"AutoCompleteParents: '{parent.Name}' achId={achId} complete={achComplete}");
                if (achComplete)
                {
                    parentEntry.Status = ItemStatus.Completed;
                    continue;
                }
            }

            var anyFound = false;
            var hasInProgress = false;
            var allComplete = true;
            var isAnyMatch = parent.Category is ContentCategory.Chocobo or ContentCategory.PvP;

            foreach (var questId in parent.UnlockQuestIds)
            {
                if (!questIdToItem.TryGetValue(questId, out var questItem))
                {
                    if (!isAnyMatch)
                        allComplete = false;
                    continue;
                }

                anyFound = true;
                var questStatus = _store.GetOrCreate(questItem.Id).Status;

                if (questStatus == ItemStatus.Completed && isAnyMatch)
                {
                    parentEntry.Status = ItemStatus.Completed;
                    goto nextParent;
                }

                if (questStatus == ItemStatus.NotStarted)
                    allComplete = false;
                else if (questStatus == ItemStatus.InProgress)
                    hasInProgress = true;
            }

            if (!anyFound)
                continue;

            if (allComplete)
                parentEntry.Status = ItemStatus.Unlocked;
            else if (hasInProgress)
                parentEntry.Status = ItemStatus.InProgress;
            else
                parentEntry.Status = ItemStatus.NotStarted;

            nextParent:;
        }
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

        if (hasAchievementCheck && Plugin.UnlockState.IsAchievementListLoaded)
        {
            var achId = item.AchievementId!.Value;
            var isComplete = IsAchievementComplete(achId);
            Plugin.Log.Debug($"ScanItem: '{item.Name}' achId={achId} isComplete={isComplete}");
            entry.Status = isComplete
                ? ItemStatus.Completed
                : ItemStatus.NotStarted;
            return;
        }

        if (hasQuestCheck)
        {
            if (QuestHelper.IsQuestComplete(item.QuestId!.Value))
                entry.Status = ItemStatus.Completed;
            else if (QuestHelper.IsQuestInProgress(item.QuestId.Value))
                entry.Status = ItemStatus.InProgress;
            else
                entry.Status = ItemStatus.NotStarted;
        }
    }

    private static bool IsAchievementComplete(uint achievementId)
    {
        if (!Plugin.UnlockState.IsAchievementListLoaded)
        {
            Plugin.Log.Debug($"IsAchievementComplete({achievementId}): list not loaded");
            return false;
        }
        var sheet = Plugin.DataManager.GameData.GetExcelSheet<Achievement>();
        if (sheet == null)
        {
            Plugin.Log.Debug($"IsAchievementComplete({achievementId}): sheet is null");
            return false;
        }
        if (!sheet.TryGetRow(achievementId, out var row))
        {
            Plugin.Log.Debug($"IsAchievementComplete({achievementId}): row not found in sheet");
            return false;
        }
        var result = Plugin.UnlockState.IsAchievementComplete(row);
        Plugin.Log.Debug($"IsAchievementComplete({achievementId}): result={result}");
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer?.Dispose();
    }
}
