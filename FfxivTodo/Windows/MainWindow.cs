using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FfxivTodo.Models;
using FfxivTodo.Services;
using ImGuiNET;

namespace FfxivTodo.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly ContentManager _contentManager;
    private readonly ProgressStore _progressStore;
    private readonly ProgressScanner _progressScanner;
    private readonly MapFlagHelper _mapFlagHelper;

    private uint? _selectedItemId;
    private string _searchText = string.Empty;
    private Expansion? _filterExpansion;
    private ContentCategory? _filterCategory;
    private ItemStatus? _filterStatus;
    private bool _showIgnored;

    public MainWindow(
        ContentManager contentManager,
        ProgressStore progressStore,
        ProgressScanner progressScanner,
        MapFlagHelper mapFlagHelper)
        : base("FFXIV Todo")
    {
        _contentManager = contentManager;
        _progressStore = progressStore;
        _progressScanner = progressScanner;
        _mapFlagHelper = mapFlagHelper;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        DrawMenuBar();
        DrawFilters();

        ImGui.Columns(2, "mainColumns", true);
        ImGui.SetColumnWidth(0, 350);
        DrawTree();
        ImGui.NextColumn();
        DrawDetailPanel();
        ImGui.Columns(1);
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMenuBar())
            return;

        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Show Ignored", null, _showIgnored))
                _showIgnored = !_showIgnored;
            ImGui.EndMenu();
        }

        if (ImGui.MenuItem("Refresh"))
            _progressScanner.ScanAll(_contentManager.Items);

        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("About"))
                ImGui.OpenPopup("About");
            ImGui.EndMenu();
        }

        if (ImGui.BeginPopup("About"))
        {
            ImGui.Text("FFXIV Todo Plugin");
            ImGui.Text("Tracks non-MSQ content completion across all expansions.");
            ImGui.Text($"Data Version: {_contentManager.DataVersion}");
            ImGui.EndPopup();
        }

        ImGui.EndMenuBar();
    }

    private void DrawFilters()
    {
        DrawEnumCombo("Expansion", ref _filterExpansion);
        ImGui.SameLine();
        DrawEnumCombo("Category", ref _filterCategory);
        ImGui.SameLine();
        DrawEnumCombo("Status", ref _filterStatus);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        ImGui.InputTextWithHint("##search", "Search...", ref _searchText, 100);
    }

    private void DrawTree()
    {
        var expansions = _contentManager.GetGroupedByExpansion()
            .Where(g => _filterExpansion == null || g.Key == _filterExpansion);

        foreach (var expGroup in expansions)
        {
            if (!ImGui.TreeNodeEx($"{expGroup.Key}##exp", ImGuiTreeNodeFlags.DefaultOpen))
                continue;

            var categories = expGroup
                .GroupBy(i => i.Category)
                .Where(g => _filterCategory == null || g.Key == _filterCategory);

            foreach (var catGroup in categories)
            {
                var filteredItems = FilterItems(catGroup);
                var items = filteredItems.ToList();

                if (!ImGui.TreeNodeEx($"{catGroup.Key} ({items.Count})##cat", ImGuiTreeNodeFlags.DefaultOpen))
                    continue;

                foreach (var item in items)
                {
                    DrawTreeItem(item);
                }

                ImGui.TreePop();
            }

            ImGui.TreePop();
        }
    }

    private IEnumerable<ContentItem> FilterItems(IEnumerable<ContentItem> items)
    {
        foreach (var item in items)
        {
            var entry = _progressStore.GetOrCreate(item.Id);

            if (entry.IsIgnored && !_showIgnored)
                continue;

            if (_filterStatus.HasValue)
            {
                var displayStatus = _contentManager.IsLocked(item.Id)
                    ? ItemStatus.NotStarted
                    : entry.Status;
                if (displayStatus != _filterStatus.Value)
                    continue;
            }

            if (!string.IsNullOrEmpty(_searchText) &&
                !item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return item;
        }
    }

    private void DrawTreeItem(ContentItem item)
    {
        var entry = _progressStore.GetOrCreate(item.Id);
        var locked = _contentManager.IsLocked(item.Id);
        var statusIcon = GetStatusIcon(entry, locked);
        var displayName = entry.IsIgnored ? $"(ignored) {item.Name}" : item.Name;
        var color = entry.IsManual ? new Vector4(0.7f, 0.7f, 1.0f, 1.0f) : GetStatusColor(entry.Status, locked);

        var flags = locked ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.DefaultOpen;

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        var isSelected = _selectedItemId == item.Id;

        if (ImGui.Selectable($"{statusIcon} {displayName}##{item.Id}", isSelected))
            _selectedItemId = item.Id;
        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text($"Lv.{item.Level}  {item.Name}");
            ImGui.Text($"Category: {item.Category}");
            if (locked)
            {
                ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "LOCKED - prerequisites not met");
                var prereqs = _contentManager.GetPrerequisites(item.Id);
                foreach (var p in prereqs)
                {
                    var pEntry = _progressStore.GetOrCreate(p.Id);
                    ImGui.Text($"  Requires: {p.Name} [{pEntry.Status}]");
                }
            }
            ImGui.EndTooltip();
        }

        if (ImGui.BeginPopupContextItem($"ctx_{item.Id}"))
        {
            DrawContextMenu(item);
            ImGui.EndPopup();
        }
    }

    private void DrawContextMenu(ContentItem item)
    {
        var entry = _progressStore.GetOrCreate(item.Id);

        if (ImGui.MenuItem(entry.IsTracked ? "Untrack" : "Track"))
        {
            _progressStore.SetTracked(item.Id, !entry.IsTracked);
            _progressStore.Save();
        }

        if (ImGui.MenuItem(entry.IsIgnored ? "Unignore" : "Ignore"))
        {
            _progressStore.SetIgnored(item.Id, !entry.IsIgnored);
            _progressStore.Save();
        }

        ImGui.Separator();

        if (ImGui.MenuItem("Flag on Map"))
            _mapFlagHelper.PlaceFlag(item);

        if (!string.IsNullOrEmpty(item.WikiUrl) && ImGui.MenuItem("Open Wiki"))
            Process.Start(new ProcessStartInfo(item.WikiUrl) { UseShellExecute = true });

        ImGui.Separator();

        if (entry.Status != ItemStatus.Completed &&
            ImGui.MenuItem("Mark as Complete"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.Completed, true);
            _progressStore.Save();
        }

        if (entry.Status != ItemStatus.NotStarted &&
            ImGui.MenuItem("Reset to Not Started"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.NotStarted, true);
            _progressStore.Save();
        }

        if (entry.IsManual && ImGui.MenuItem("Reset to Auto"))
        {
            _progressStore.ClearManualFlag(item.Id);
            _progressStore.Save();
        }
    }

    private void DrawDetailPanel()
    {
        if (!_selectedItemId.HasValue)
        {
            ImGui.Text("Select an item to view details");
            return;
        }

        var item = _contentManager.Items.FirstOrDefault(i => i.Id == _selectedItemId.Value);
        if (item == null)
            return;

        var entry = _progressStore.GetOrCreate(item.Id);
        var locked = _contentManager.IsLocked(item.Id);

        ImGui.Text($"{item.Name}");
        ImGui.Separator();

        ImGui.Text($"Level: {item.Level}");
        ImGui.Text($"Expansion: {item.Expansion}");
        ImGui.Text($"Category: {item.Category}");
        ImGui.Text($"Status: {entry.Status}");
        if (entry.IsManual)
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 1.0f, 1), "(manual override)");

        if (locked)
        {
            ImGui.Separator();
            ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Prerequisites:");
            var prereqs = _contentManager.GetPrerequisites(item.Id);
            foreach (var p in prereqs)
            {
                var pEntry = _progressStore.GetOrCreate(p.Id);
                var icon = GetStatusIcon(pEntry, false);
                ImGui.Text($"  {icon} {p.Name} (Lv.{p.Level})");
            }
        }

        ImGui.Separator();

        if (ImGui.Button("Track"))
        {
            _progressStore.SetTracked(item.Id, true);
            _progressStore.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Untrack"))
        {
            _progressStore.SetTracked(item.Id, false);
            _progressStore.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button(entry.IsIgnored ? "Unignore" : "Ignore"))
        {
            _progressStore.SetIgnored(item.Id, !entry.IsIgnored);
            _progressStore.Save();
        }

        var canFlag = item.LocationTerritoryId.HasValue && item.LocationTerritoryId != 0;
        if (!canFlag)
            ImGui.BeginDisabled();
        if (ImGui.Button("Flag on Map"))
            _mapFlagHelper.PlaceFlag(item);
        if (!canFlag)
            ImGui.EndDisabled();

        if (!string.IsNullOrEmpty(item.WikiUrl))
        {
            ImGui.SameLine();
            if (ImGui.Button("Open Wiki"))
                Process.Start(new ProcessStartInfo(item.WikiUrl) { UseShellExecute = true });
        }

        ImGui.Separator();

        if (ImGui.Button("Mark Complete"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.Completed, true);
            _progressStore.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.NotStarted, true);
            _progressStore.Save();
        }
        if (entry.IsManual)
        {
            ImGui.SameLine();
            if (ImGui.Button("Reset to Auto"))
            {
                _progressStore.ClearManualFlag(item.Id);
                _progressStore.Save();
            }
        }
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

    private static void DrawEnumCombo<T>(string label, ref T? value) where T : struct, Enum
    {
        ImGui.SetNextItemWidth(120);
        if (!ImGui.BeginCombo($"{label}##{label}", value?.ToString() ?? "All"))
            return;

        if (ImGui.Selectable("All", !value.HasValue))
            value = null;

        foreach (var val in Enum.GetValues<T>())
        {
            if (ImGui.Selectable(val.ToString(), value.HasValue && value.Value.Equals(val)))
                value = val;
        }

        ImGui.EndCombo();
    }

    public void Dispose() { }
}