using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FfxivTodo.Models;
using FfxivTodo.Services;
using ImGuiNET;

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
        : base("Todo", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                         ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
                         ImGuiWindowFlags.AlwaysAutoResize)
    {
        _contentManager = contentManager;
        _progressStore = progressStore;
        _mapFlagHelper = mapFlagHelper;

        IsOpen = true;
    }

    public override void Draw()
    {
        var config = Plugin.PluginInterface.GetPluginConfig() as Configuration;
        if (config == null)
            return;

        Position = config.OverlayPosition;

        if (config.OverlayLocked)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }

        var alpha = config.OverlayOpacity / 100f;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);

        var fontScale = config.OverlayFontScale;
        if (Math.Abs(fontScale - 1.0f) > 0.01f)
            ImGui.SetWindowFontScale(fontScale);

        ImGui.BeginChild("tracked_list", new Vector2(config.OverlayWidth, 0));

        var trackedItems = _contentManager.GetGroupedByExpansion()
            .SelectMany(g => g)
            .Where(i =>
            {
                var entry = _progressStore.GetOrCreate(i.Id);
                return entry.IsTracked && !entry.IsIgnored;
            })
            .OrderBy(i => i.Expansion)
            .ThenBy(i => i.Level)
            .Take(config.OverlayMaxItems);

        foreach (var item in trackedItems)
        {
            var entry = _progressStore.GetOrCreate(item.Id);
            var locked = _contentManager.IsLocked(item.Id);
            var statusIcon = GetStatusIcon(entry, locked);
            var color = GetStatusColor(entry.Status, locked);

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text($"{statusIcon} {Truncate(item.Name, 30)}  {item.Expansion} Lv{item.Level}");
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(item.Name);
                ImGui.Text($"Category: {item.Category}");
                ImGui.Text($"Status: {entry.Status}");
                if (locked)
                {
                    ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Locked");
                    var prereqs = _contentManager.GetPrerequisites(item.Id);
                    foreach (var p in prereqs)
                    {
                        var pEntry = _progressStore.GetOrCreate(p.Id);
                        ImGui.Text($"  Requires: {p.Name} [{pEntry.Status}]");
                    }
                }
                ImGui.EndTooltip();
            }

            if (ImGui.BeginPopupContextItem($"overlay_ctx_{item.Id}"))
            {
                var canFlag = item.LocationTerritoryId.HasValue && item.LocationTerritoryId != 0;
                if (!canFlag) ImGui.BeginDisabled();
                if (ImGui.MenuItem("Flag on Map"))
                    _mapFlagHelper.PlaceFlag(item);
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

    private static string GetStatusIcon(ProgressEntry entry, bool locked)
    {
        if (locked) return "[!]";
        return entry.Status switch
        {
            ItemStatus.Completed => "[\u2713]",
            ItemStatus.InProgress => "[~]",
            _ => "[ ]"
        };
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