using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FfxivTodo.Models;
using FfxivTodo.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace FfxivTodo.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly ContentManager _contentManager;
    private readonly ProgressStore _progressStore;
    private readonly ProgressScanner _progressScanner;
    private readonly MapFlagHelper _mapFlagHelper;
    private readonly OverlayWindow _overlayWindow;

    private uint? _selectedItemId;
    private string _searchText = string.Empty;
    private readonly HashSet<Expansion> _selectedExpansions = [];
    private readonly HashSet<ContentCategory> _selectedCategories = [];
    private readonly HashSet<FilterState> _selectedStates = [];
    private bool _firstDraw = true;
    private bool _filterDirty;
    private readonly string _filterFilePath;
    private readonly HashSet<uint> _expandedChains = [];

    public MainWindow(
        ContentManager contentManager,
        ProgressStore progressStore,
        ProgressScanner progressScanner,
        MapFlagHelper mapFlagHelper,
        OverlayWindow overlayWindow,
        string configDirectory)
        : base("FFXIV Todo")
    {
        _contentManager = contentManager;
        _progressStore = progressStore;
        _progressScanner = progressScanner;
        _mapFlagHelper = mapFlagHelper;
        _overlayWindow = overlayWindow;
        _filterFilePath = Path.Combine(configDirectory, "filters.json");

        LoadFilterState();

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    private void LoadFilterState()
    {
        if (!File.Exists(_filterFilePath)) return;

        try
        {
            var json = File.ReadAllText(_filterFilePath);
            var data = JsonConvert.DeserializeObject<FilterStateModel>(json);
            if (data == null) return;

            _searchText = data.SearchText ?? string.Empty;
            if (data.Expansions != null)
                _selectedExpansions.UnionWith(data.Expansions);
            if (data.Categories != null)
                _selectedCategories.UnionWith(data.Categories);
            if (data.States != null)
                _selectedStates.UnionWith(data.States);
        }
        catch { }
    }

    private void SaveFilterState()
    {
        var data = new FilterStateModel
        {
            SearchText = _searchText,
            Expansions = _selectedExpansions.ToList(),
            Categories = _selectedCategories.ToList(),
            States = _selectedStates.ToList(),
        };

        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(_filterFilePath, json);
    }

    private sealed class FilterStateModel
    {
        public string? SearchText { get; set; }
        public List<Expansion>? Expansions { get; set; }
        public List<ContentCategory>? Categories { get; set; }
        public List<FilterState>? States { get; set; }
    }

    public override void Draw()
    {
        if (_firstDraw)
        {
            _firstDraw = false;
            Plugin.Log.Information($"MainWindow.Draw() first call: Items={_contentManager.Items.Count}, IsOpen={IsOpen}");
        }

        ImGui.Text($"Content items loaded: {_contentManager.Items.Count}");
        ImGui.Separator();

        DrawMenuBar();
        DrawFilters();

        if (ImGui.BeginTable("mainColumns", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("##tree", ImGuiTableColumnFlags.WidthFixed, 350);
            ImGui.TableSetupColumn("##details", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            ImGui.BeginChild("treeScroll");
            DrawTree();
            ImGui.EndChild();
            ImGui.TableNextColumn();
            ImGui.BeginChild("detailsScroll");
            DrawDetailPanel();
            ImGui.EndChild();
            ImGui.EndTable();
        }

        if (_filterDirty)
        {
            _filterDirty = false;
            SaveFilterState();
        }
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMenuBar())
            return;

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
        DrawMultiSelectFilter(
            "Expansion",
            _selectedExpansions,
            MainWindowFilterLogic.GetExpansionLabel);
        ImGui.SameLine();
        DrawMultiSelectFilter(
            "Category",
            _selectedCategories,
            MainWindowFilterLogic.GetCategoryLabel);

        DrawStateChips();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        ImGui.InputTextWithHint("##search", "Search...", ref _searchText, 100);
        ImGui.SameLine();
        if (ImGui.Button("Clear filters"))
            ClearFilters();
    }

    private void DrawMultiSelectFilter<T>(
        string label,
        HashSet<T> selected,
        Func<T, string> getLabel) where T : struct, Enum
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine();

        var summary = MainWindowFilterLogic.GetSummary(selected, getLabel, "All");
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo($"##{label}", summary))
        {
            if (ImGui.Selectable("All", selected.Count == 0))
            {
                selected.Clear();
                _filterDirty = true;
            }

            foreach (var value in Enum.GetValues<T>())
            {
                var isSelected = selected.Contains(value);
                if (ImGui.Selectable(getLabel(value), isSelected))
                {
                    if (!selected.Add(value))
                        selected.Remove(value);
                    _filterDirty = true;
                }
            }

            ImGui.EndCombo();
        }
    }

    private void DrawStateChips()
    {
        var states = new[] { FilterState.NotStarted, FilterState.InProgress, FilterState.Completed, FilterState.Locked, FilterState.Ignored };

        foreach (var state in states)
        {
            var selected = _selectedStates.Contains(state);
            if (selected)
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.25f, 0.45f, 0.25f, 1f));

            if (ImGui.SmallButton(MainWindowFilterLogic.GetStateLabel(state)))
            {
                if (!selected)
                    _selectedStates.Add(state);
                else
                    _selectedStates.Remove(state);
                _filterDirty = true;
            }

            if (selected)
                ImGui.PopStyleColor();

            ImGui.SameLine();
        }
    }

    private void DrawTree()
    {
        var expansions = _contentManager.GetGroupedByExpansion()
            .Where(g => MainWindowFilterLogic.MatchesExpansion(g.Key, _selectedExpansions));

        foreach (var expGroup in expansions)
        {
            var expLabel = MainWindowFilterLogic.GetExpansionLabel(expGroup.Key);
            if (!ImGui.TreeNodeEx($"{expLabel}##exp", ImGuiTreeNodeFlags.DefaultOpen))
                continue;

            var categories = expGroup
                .GroupBy(i => i.Category)
                .Where(g => MainWindowFilterLogic.MatchesCategory(g.Key, _selectedCategories));

            foreach (var catGroup in categories)
            {
                var filteredItems = FilterItems(catGroup);
                var items = filteredItems.ToList();

                if (items.Count == 0)
                    continue;

                var catLabel = MainWindowFilterLogic.GetCategoryLabel(catGroup.Key);
                if (!ImGui.TreeNodeEx($"{catLabel} ({items.Count})##cat", ImGuiTreeNodeFlags.DefaultOpen))
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

            var locked = _contentManager.IsLocked(item.Id);
            var displayState = MainWindowFilterLogic.GetDisplayState(entry, locked);

            if (!MainWindowFilterLogic.MatchesState(displayState, _selectedStates))
                continue;

            if (!string.IsNullOrEmpty(_searchText))
            {
                var matchesName = item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
                var matchesQuest = false;
                if (!matchesName)
                {
                    var quests = _contentManager.GetUnlockQuests(item.Id);
                    matchesQuest = quests.Any(q =>
                        q.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
                }

                if (!matchesName && !matchesQuest)
                    continue;
            }

            yield return item;
        }
    }

    private void DrawTreeItem(ContentItem item)
    {
        var entry = _progressStore.GetOrCreate(item.Id);
        var locked = _contentManager.IsLocked(item.Id);
        var displayName = entry.IsIgnored ? $"(ignored) {item.Name}" : item.Name;
        var color = entry.IsManual ? new Vector4(0.7f, 0.7f, 1.0f, 1.0f) : GetStatusColor(entry.Status, locked);

        ImGui.PushID((int)item.Id);

        var isTracked = entry.IsTracked;
        var val = isTracked;
        if (ImGui.Checkbox("##cb", ref val) && val != isTracked)
        {
            _progressStore.SetTracked(item.Id, val);
            _progressStore.Save();
            if (val) _overlayWindow.IsOpen = true;
        }

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        var isSelected = _selectedItemId == item.Id;

        if (ImGui.Selectable($"{displayName}##name", isSelected))
            _selectedItemId = item.Id;
        ImGui.PopStyleColor();

        ImGui.PopID();

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

        if (item.UnlockQuestIds.Length > 0)
        {
            var quests = _contentManager.GetUnlockQuests(item.Id);
            var nextQuest = quests.FirstOrDefault(q =>
            {
                var qe = _progressStore.GetOrCreate(q.Id);
                return qe.Status != ItemStatus.Completed;
            });

            if (nextQuest != null)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1), $"→ {nextQuest.Name}");
            }

            var isExpanded = _expandedChains.Contains(item.Id);
            if (isExpanded)
                ImGui.SetNextItemOpen(true);
            if (ImGui.TreeNodeEx($"##chain_{item.Id}", ImGuiTreeNodeFlags.None))
            {
                _expandedChains.Add(item.Id);

                foreach (var quest in quests)
                {
                    var qe = _progressStore.GetOrCreate(quest.Id);
                    var qLocked = _contentManager.IsLocked(quest.Id);
                    var qColor = GetStatusColor(qe.Status, qLocked);

                    ImGui.PushID((int)quest.Id);
                    ImGui.Indent();

                    ImGui.PushStyleColor(ImGuiCol.Text, qColor);
                    var qSelected = _selectedItemId == quest.Id;
                    if (ImGui.Selectable($"{quest.Name}##qname", qSelected))
                        _selectedItemId = quest.Id;
                    ImGui.PopStyleColor();

                    ImGui.Unindent();
                    ImGui.PopID();
                }

                ImGui.TreePop();
            }
            else
            {
                _expandedChains.Remove(item.Id);
            }
        }
    }

    private void DrawContextMenu(ContentItem item)
    {
        var entry = _progressStore.GetOrCreate(item.Id);

        if (ImGui.MenuItem(entry.IsTracked ? "Untrack" : "Track"))
        {
            var wasUntracked = !entry.IsTracked;
            _progressStore.SetTracked(item.Id, !entry.IsTracked);
            _progressStore.Save();
            if (wasUntracked) _overlayWindow.IsOpen = true;
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
            _progressStore.SetStatus(item.Id, ItemStatus.Completed, true, _contentManager.Items);
            _progressStore.Save();
        }

        if (entry.Status != ItemStatus.NotStarted &&
            ImGui.MenuItem("Reset to Not Started"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.NotStarted, true, _contentManager.Items);
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

        if (item.UnlockQuestIds.Length > 0)
        {
            ImGui.Separator();
            ImGui.Text("Unlock Quest Chain:");
            var quests = _contentManager.GetUnlockQuests(item.Id);
            foreach (var quest in quests)
            {
                var qe = _progressStore.GetOrCreate(quest.Id);
                var icon = GetStatusIcon(qe, _contentManager.IsLocked(quest.Id));
                ImGui.Text($"  {icon} {quest.Name} (Lv.{quest.Level})");
            }
        }

        ImGui.Separator();

        if (entry.IsTracked ? ImGui.Button("Untrack") : ImGui.Button("Track"))
        {
            var wasUntracked = !entry.IsTracked;
            _progressStore.SetTracked(item.Id, !entry.IsTracked);
            _progressStore.Save();
            if (wasUntracked) _overlayWindow.IsOpen = true;
        }
        ImGui.SameLine();
        if (ImGui.Button(entry.IsIgnored ? "Unignore" : "Ignore"))
        {
            _progressStore.SetIgnored(item.Id, !entry.IsIgnored);
            _progressStore.Save();
        }

        var canFlag = (item.LocationTerritoryId.HasValue && item.LocationTerritoryId.Value != 0) ||
                       (item.LocationMapX.HasValue && !string.IsNullOrEmpty(item.LocationTerritoryName));
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
            _progressStore.SetStatus(item.Id, ItemStatus.Completed, true, _contentManager.Items);
            _progressStore.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.NotStarted, true, _contentManager.Items);
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

    private void ClearFilters()
    {
        _selectedExpansions.Clear();
        _selectedCategories.Clear();
        _selectedStates.Clear();
        _searchText = string.Empty;
        _filterDirty = true;
    }

    public void Dispose() { }
}