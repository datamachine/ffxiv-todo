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
using Lumina.Excel.Sheets;
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
    private int _forceTreeOpen;
    private readonly string _filterFilePath;
    

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
        }

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4);

        DrawProgressSummary();

        DrawMenuBar();
        DrawFilters();

        if (ImGui.BeginTable("mainColumns", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("##tree", ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableSetupColumn("##details", ImGuiTableColumnFlags.WidthStretch, 0.65f);

            ImGui.TableNextColumn();
            var treeBg = ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg];
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(treeBg.X * 0.9f, treeBg.Y * 0.9f, treeBg.Z * 0.9f, treeBg.W));
            ImGui.BeginChild("treeScroll");
            DrawTree();
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(treeBg.X * 1.05f, treeBg.Y * 1.05f, treeBg.Z * 1.05f, treeBg.W));
            ImGui.BeginChild("detailsScroll");
            DrawDetailPanel();
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.EndTable();
        }

        ImGui.PopStyleVar(2);

        if (_filterDirty)
        {
            _filterDirty = false;
            SaveFilterState();
        }
    }

    private void DrawProgressSummary()
    {
        var total = _contentManager.Items.Count(i => i.Category != ContentCategory.BlueUnlock);
        var completed = _contentManager.Items.Count(i =>
            i.Category != ContentCategory.BlueUnlock &&
            _progressStore.GetOrCreate(i.Id).Status == ItemStatus.Completed);
        var pct = total > 0 ? (float)completed / total : 0;

        ImGui.Text($"Progress: {completed}/{total} ({pct * 100:F1}%)");
        ImGui.SameLine();
        ImGui.ProgressBar(pct, new Vector2(-1, ImGui.GetTextLineHeight()), "");

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            var inProgress = _contentManager.Items.Count(i =>
                i.Category != ContentCategory.BlueUnlock &&
                _progressStore.GetOrCreate(i.Id).Status == ItemStatus.InProgress);
            var unlocked = _contentManager.Items.Count(i =>
                i.Category != ContentCategory.BlueUnlock &&
                _progressStore.GetOrCreate(i.Id).Status == ItemStatus.Unlocked);
            ImGui.Text($"Completed: {completed}");
            ImGui.Text($"Unlocked: {unlocked}");
            ImGui.Text($"In Progress: {inProgress}");
            ImGui.Text($"Remaining: {total - completed - unlocked - inProgress}");
            ImGui.EndTooltip();
        }

        ImGui.Separator();
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
        ImGui.SameLine(0, 16);
        DrawMultiSelectFilter(
            "Category",
            _selectedCategories,
            MainWindowFilterLogic.GetCategoryLabel);

        ImGui.SameLine(0, 16);
        DrawStateChips();

        ImGui.SameLine(0, 16);
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
        var states = new[] { FilterState.NotStarted, FilterState.InProgress, FilterState.Unlocked, FilterState.Completed, FilterState.Locked, FilterState.Ignored };

        foreach (var state in states)
        {
            var selected = _selectedStates.Contains(state);
            if (selected)
            {
                var chipColor = MainWindowFilterLogic.GetFilterStateColor(state);
                ImGui.PushStyleColor(ImGuiCol.Button, chipColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, chipColor with { W = 1f });
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, chipColor with { W = 0.9f });
            }

            if (ImGui.SmallButton(MainWindowFilterLogic.GetStateLabel(state)))
            {
                if (!selected)
                    _selectedStates.Add(state);
                else
                    _selectedStates.Remove(state);
                _filterDirty = true;
            }

            if (selected)
            {
                ImGui.PopStyleColor(3);
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DrawTree()
    {
        if (ImGui.Button("Expand All"))
            _forceTreeOpen = 1;
        ImGui.SameLine();
        if (ImGui.Button("Collapse All"))
            _forceTreeOpen = -1;
        ImGui.SameLine();
        if (ImGui.Button("Re-scan Progress"))
            _progressScanner.ScanAll(_contentManager.Items);

        var expansions = _contentManager.GetGroupedByExpansion()
            .Where(g => MainWindowFilterLogic.MatchesExpansion(g.Key, _selectedExpansions));

        foreach (var expGroup in expansions)
        {
            var expItems = expGroup.Where(i => i.Category != ContentCategory.BlueUnlock).ToList();
            var expCompleted = expItems.Count(i => _progressStore.GetOrCreate(i.Id).Status == ItemStatus.Completed);
            var expLabel = $"{MainWindowFilterLogic.GetExpansionLabel(expGroup.Key)} [{expCompleted}/{expItems.Count}]";

            if (_forceTreeOpen == 1 || _forceTreeOpen == -1)
                ImGui.SetNextItemOpen(true);
            else if (_forceTreeOpen == -2)
                ImGui.SetNextItemOpen(false);

            if (!ImGui.TreeNodeEx($"{expLabel}##exp", ImGuiTreeNodeFlags.DefaultOpen))
                continue;

            var categories = expGroup
                .GroupBy(i => i.Category)
                .Where(g => MainWindowFilterLogic.MatchesCategory(g.Key, _selectedCategories));

            foreach (var catGroup in categories)
            {
                if (catGroup.Key == ContentCategory.BlueUnlock)
                    continue;

                var filteredItems = FilterItems(catGroup);
                var items = filteredItems.ToList();

                if (items.Count == 0)
                    continue;

                var catCompleted = items.Count(i => _progressStore.GetOrCreate(i.Id).Status == ItemStatus.Completed);
                var catLabel = $"{MainWindowFilterLogic.GetCategoryLabel(catGroup.Key)} [{catCompleted}/{items.Count}]";

                if (_forceTreeOpen == 1)
                    ImGui.SetNextItemOpen(true);
                else if (_forceTreeOpen == -1 || _forceTreeOpen == -2)
                    ImGui.SetNextItemOpen(false);

                if (!ImGui.TreeNodeEx($"{catLabel}##cat", ImGuiTreeNodeFlags.DefaultOpen))
                    continue;

                foreach (var item in items)
                {
                    DrawTreeItem(item);
                }

                ImGui.TreePop();
            }

            ImGui.TreePop();
        }

        if (_forceTreeOpen == 1)
            _forceTreeOpen = 0;
        else if (_forceTreeOpen == -1)
            _forceTreeOpen = -2;
        else if (_forceTreeOpen == -2)
            _forceTreeOpen = 0;
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
        var color = MainWindowFilterLogic.GetStatusColor(entry.Status, locked, entry.IsManual);

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
            ImGui.Text(MainWindowFilterLogic.GetCategoryLabel(item.Category));
            if (locked)
            {
                ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "LOCKED - prerequisites not met");
                var prereqs = _contentManager.GetPrerequisites(item.Id);
                foreach (var p in prereqs)
                {
                    var pEntry = _progressStore.GetOrCreate(p.Id);
                    var pLocked = _contentManager.IsLocked(p.Id);
                    var icon = MainWindowFilterLogic.GetStatusIcon(pEntry, pLocked);
                    ImGui.Text($"  {icon} {p.Name}");
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
        {
            var quests = _contentManager.GetUnlockQuests(item.Id);
            _mapFlagHelper.PlaceFlag(_mapFlagHelper.GetFlagTarget(item, quests, id => _progressStore.GetOrCreate(id).Status));
        }

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

        var color = MainWindowFilterLogic.GetStatusColor(entry.Status, locked, entry.IsManual);
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text($"{item.Name}");
        ImGui.PopStyleColor();
        ImGui.Separator();

        ImGui.Text($"Level: {item.Level}  |  {MainWindowFilterLogic.GetExpansionLabel(item.Expansion)}");
        ImGui.Text($"{MainWindowFilterLogic.GetCategoryLabel(item.Category)}  |  {MainWindowFilterLogic.GetStatusLabel(entry.Status)}");
        if (entry.IsManual)
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 1.0f, 1), "(manual override)");

        if (item.AchievementId.HasValue)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Achievement:");
            var achId = item.AchievementId.Value;
            var achName = $"ID {achId}";
            var sheet = Plugin.DataManager.GameData.GetExcelSheet<Achievement>();
            if (sheet?.TryGetRow(achId, out var achRow) == true)
                achName = achRow.Name.ExtractText();
            ImGui.Text($"  {achName}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Wiki"))
            {
                var wikiUrl = $"https://ffxiv.consolegameswiki.com/wiki/{achName.Replace(" ", "_")}";
                Process.Start(new ProcessStartInfo(wikiUrl) { UseShellExecute = true });
            }
        }

        if (locked)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Prerequisites:");
            var prereqs = _contentManager.GetPrerequisites(item.Id);
            foreach (var p in prereqs)
            {
                var pEntry = _progressStore.GetOrCreate(p.Id);
                var pLocked = _contentManager.IsLocked(p.Id);
                var icon = MainWindowFilterLogic.GetStatusIcon(pEntry, pLocked);
                var pColor = MainWindowFilterLogic.GetStatusColor(pEntry.Status, pLocked);

                ImGui.PushStyleColor(ImGuiCol.Text, pColor);
                var clicked = ImGui.Selectable($"  {icon} {p.Name} (Lv.{p.Level})##prereq_{p.Id}");
                ImGui.PopStyleColor();
                if (clicked)
                    _selectedItemId = p.Id;
            }
        }

        if (item.UnlockQuestIds.Length > 0)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Unlock Quest Chain:");
            var quests = _contentManager.GetUnlockQuests(item.Id);
            var groups = quests
                .Select(q => (Quest: q, Entry: _progressStore.GetOrCreate(q.Id)))
                .GroupBy(x => x.Entry.Status)
                .OrderBy(g => g.Key == ItemStatus.Completed ? 0 :
                               g.Key == ItemStatus.Unlocked ? 1 :
                               g.Key == ItemStatus.InProgress ? 2 : 3);

            foreach (var group in groups)
            {
                var label = MainWindowFilterLogic.GetStatusLabel(group.Key);
                var labelColor = MainWindowFilterLogic.GetStatusColor(group.Key, false);
                ImGui.TextColored(labelColor, $"  {label}:");

                foreach (var (quest, qe) in group)
                {
                    var qLocked = _contentManager.IsLocked(quest.Id);
                    var icon = MainWindowFilterLogic.GetStatusIcon(qe, qLocked);
                    var qColor = MainWindowFilterLogic.GetStatusColor(qe.Status, qLocked);
                    ImGui.TextColored(qColor, $"    {icon} {quest.Name} (Lv.{quest.Level})");
                }
            }
        }

        ImGui.Spacing();
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

        var chainQuests = _contentManager.GetUnlockQuests(item.Id);
        var flagTarget = _mapFlagHelper.GetFlagTarget(item, chainQuests, id => _progressStore.GetOrCreate(id).Status);
        var canFlag = (flagTarget.LocationTerritoryId.HasValue && flagTarget.LocationTerritoryId.Value != 0) ||
                       (flagTarget.LocationMapX.HasValue && !string.IsNullOrEmpty(flagTarget.LocationTerritoryName));
        if (!canFlag)
            ImGui.BeginDisabled();
        if (ImGui.Button("Flag on Map"))
            _mapFlagHelper.PlaceFlag(flagTarget);
        if (!canFlag)
            ImGui.EndDisabled();

        if (!string.IsNullOrEmpty(item.WikiUrl))
        {
            ImGui.SameLine();
            if (ImGui.Button("Open Wiki"))
                Process.Start(new ProcessStartInfo(item.WikiUrl) { UseShellExecute = true });
        }

        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.45f, 0.3f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.55f, 0.35f, 1f));
        if (ImGui.Button("Mark Complete"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.Completed, true, _contentManager.Items);
            _progressStore.Save();
        }
        ImGui.PopStyleColor(2);
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.45f, 0.25f, 0.25f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.55f, 0.3f, 0.3f, 1f));
        if (ImGui.Button("Reset"))
        {
            _progressStore.SetStatus(item.Id, ItemStatus.NotStarted, true, _contentManager.Items);
            _progressStore.Save();
        }
        ImGui.PopStyleColor(2);
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