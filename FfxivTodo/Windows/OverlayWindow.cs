using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FfxivTodo.Models;
using FfxivTodo.Services;
using Dalamud.Bindings.ImGui;

namespace FfxivTodo.Windows;

public sealed class OverlayWindow : Window, IDisposable
{
    private readonly ContentManager _contentManager;
    private readonly ProgressStore _progressStore;
    private readonly MapFlagHelper _mapFlagHelper;

    public OverlayWindow(
        ContentManager contentManager,
        ProgressStore progressStore,
        MapFlagHelper mapFlagHelper)
        : base("Tracked Quests")
    {
        _contentManager = contentManager;
        _progressStore = progressStore;
        _mapFlagHelper = mapFlagHelper;

        Size = new Vector2(350, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var config = Plugin.Configuration;

        if (config.OverlayLocked)
            Flags |= ImGuiWindowFlags.NoMove;
        else
            Flags &= ~ImGuiWindowFlags.NoMove;

        var alpha = config.OverlayOpacity / 100f;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);

        var fontScale = config.OverlayFontScale;
        if (Math.Abs(fontScale - 1.0f) > 0.01f)
            ImGui.SetWindowFontScale(fontScale);

        ImGui.BeginChild("tracked_list", new Vector2(config.OverlayWidth, 0), true);

        var allItems = _contentManager.Items;
        var trackedCount = 0;
        foreach (var i in allItems)
        {
            var entry = _progressStore.GetOrCreate(i.Id);
            if (entry.IsTracked && !entry.IsIgnored)
                trackedCount++;
        }

        var trackedItems = allItems
            .Where(i =>
            {
                var entry = _progressStore.GetOrCreate(i.Id);
                return entry.IsTracked && !entry.IsIgnored;
            })
            .OrderBy(i => i.Expansion)
            .ThenBy(i => i.Level)
            .Take(config.OverlayMaxItems)
            .ToList();

        if (trackedItems.Count == 0)
        {
            ImGui.Text($"No tracked quests (loaded {allItems.Count} items, {trackedCount} tracked)");
        }

        foreach (var item in trackedItems)
        {
            var entry = _progressStore.GetOrCreate(item.Id);
            var locked = _contentManager.IsLocked(item.Id);
            var color = GetStatusColor(entry.Status, locked);

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text($"{Truncate(item.Name, 30)}  {MainWindowFilterLogic.GetExpansionLabel(item.Expansion)} Lv{item.Level}");
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(item.Name);
                ImGui.Text(MainWindowFilterLogic.GetCategoryLabel(item.Category));
                var statusLabel = entry.Status switch
                {
                    ItemStatus.Completed => "\u2713 Completed",
                    ItemStatus.InProgress => "\u25D0 In progress",
                    _ => "\u25CB Not started"
                };
                ImGui.Text(statusLabel);
                if (locked)
                {
                    ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Locked");
                    var prereqs = _contentManager.GetPrerequisites(item.Id);
                    foreach (var p in prereqs)
                    {
                        var pEntry = _progressStore.GetOrCreate(p.Id);
                        var pStatus = pEntry.Status switch
                        {
                            ItemStatus.Completed => "\u2713 Completed",
                            ItemStatus.InProgress => "\u25D0 In progress",
                            _ => "\u25CB Not started"
                        };
                        ImGui.Text($"  {pStatus} {p.Name}");
                    }
                }
                ImGui.EndTooltip();
            }

            if (ImGui.BeginPopupContextItem($"overlay_ctx_{item.Id}"))
            {
                var quests = _contentManager.GetUnlockQuests(item.Id);
                var flagTarget = _mapFlagHelper.GetFlagTarget(item, quests, id => _progressStore.GetOrCreate(id).Status);
                var canFlag = (flagTarget.LocationTerritoryId.HasValue && flagTarget.LocationTerritoryId != 0) ||
                               (flagTarget.LocationMapX.HasValue && !string.IsNullOrEmpty(flagTarget.LocationTerritoryName));
                if (!canFlag) ImGui.BeginDisabled();
                if (ImGui.MenuItem("Flag on Map"))
                    _mapFlagHelper.PlaceFlag(flagTarget);
                if (!canFlag) ImGui.EndDisabled();
                if (ImGui.MenuItem("Untrack"))
                {
                    _progressStore.SetTracked(item.Id, false);
                    _progressStore.Save();
                }
                var canComplete = entry.Status != ItemStatus.Completed;
                if (!canComplete) ImGui.BeginDisabled();
                if (ImGui.MenuItem("Mark Complete"))
                {
                    _progressStore.SetStatus(item.Id, ItemStatus.Completed, true);
                    _progressStore.Save();
                }
                if (!canComplete) ImGui.EndDisabled();
                ImGui.EndPopup();
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();

        if (Math.Abs(fontScale - 1.0f) > 0.01f)
            ImGui.SetWindowFontScale(1.0f);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static Vector4 GetStatusColor(ItemStatus status, bool locked)
    {
        if (locked) return new Vector4(0.4f, 0.4f, 0.4f, 1);
        return status switch
        {
            ItemStatus.Completed => new Vector4(0.3f, 1.0f, 0.3f, 1),
            ItemStatus.InProgress => new Vector4(1.0f, 1.0f, 0.3f, 1),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1)
        };
    }

    public void Dispose() { }
}