# FFXIV Todo Plugin — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Dalamud plugin that tracks unlocking and completion of non-MSQ content (quests, duties, systems) across all FFXIV expansions.

**Architecture:** Plugin + data layer separation. `ContentManager` loads bundled JSON, `ProgressStore` persists player progress, `ProgressScanner` auto-detects from journal/achievements. Two ImGui windows (main tree view + tracked overlay) read from `ContentManager`. Map flagging via Dalamud's map API.

**Tech Stack:** C# (.NET 8), Dalamud SDK, ImGui.NET, System.Text.Json

**Spec:** `docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md`

---

## File Structure

```
FfxivTodo/
  FfxivTodo.csproj
  FfxivTodo.json              # Dalamud manifest
  Plugin.cs                   # IDalamudPlugin entry point
  Configuration.cs            # Plugin configuration
  Models/
    ContentItem.cs            # Bundled data model
    ProgressEntry.cs          # Player state model
    Enums.cs                  # Expansion, Category, ItemStatus enums
  Services/
    ContentManager.cs         # Loads content.json, computes display states
    ProgressStore.cs          # Loads/saves progress.json
    ProgressScanner.cs        # Polls journal and achievements
    MapFlagHelper.cs          # Places in-game map flags
  Windows/
    MainWindow.cs             # Tree view + detail panel
    OverlayWindow.cs          # Tracked items overlay
  Data/
    content.json              # Embedded resource — bundled content data
```

---

### Task 1: Project Scaffolding

**Files:**
- Create: `FfxivTodo/FfxivTodo.csproj`
- Create: `FfxivTodo/FfxivTodo.json`
- Create: `FfxivTodo/Plugin.cs`
- Create: `FfxivTodo/Configuration.cs`

- [ ] **Step 1: Create project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DalamudPackager" Version="*" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="$(AppData)\XIVLauncher\addon\Hooks\dev\Dalamud\Dalamud.csproj" Private="False" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Data\content.json" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create Dalamud manifest**

```json
{
  "Author": "",
  "Name": "FFXIV Todo",
  "Punchline": "Track non-MSQ content completion",
  "Description": "Track unlocking and completion of all non-MSQ content across every expansion. Auto-detects progress from journal and achievements.",
  "InternalName": "FfxivTodo",
  "ApplicableVersion": "any",
  "Tags": ["quests", "tracking", "completion", "todo"]
}
```

- [ ] **Step 3: Create Plugin.cs skeleton**

```csharp
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using FfxivTodo.Services;
using FfxivTodo.Windows;

namespace FfxivTodo;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "FFXIV Todo";

    [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IQuestManager QuestManager { get; private set; } = null!;
    [PluginService] internal static IAchievementManager AchievementManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FfxivTodo");

    private readonly ContentManager _contentManager;
    private readonly ProgressStore _progressStore;
    private readonly ProgressScanner _progressScanner;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _contentManager = new ContentManager();
        _progressStore = new ProgressStore(PluginInterface.ConfigDirectory.FullName);
        _progressScanner = new ProgressScanner(_progressStore);

        WindowSystem.AddWindow(new MainWindow(_contentManager, _progressStore, _progressScanner));
        WindowSystem.AddWindow(new OverlayWindow(_contentManager, _progressStore));

        CommandManager.AddHandler("/todo", new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle FFXIV Todo main window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        ClientState.Login += OnLogin;
        ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler("/todo");
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        ClientState.Login -= OnLogin;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        _progressScanner.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var mainWindow = WindowSystem.GetWindow<MainWindow>()!;
        var overlayWindow = WindowSystem.GetWindow<OverlayWindow>()!;

        if (args == "overlay")
            overlayWindow.IsOpen = !overlayWindow.IsOpen;
        else
            mainWindow.IsOpen = !mainWindow.IsOpen;
    }

    private void DrawUI() => WindowSystem.Draw();
    private void DrawConfigUI() => WindowSystem.GetWindow<MainWindow>()!.IsOpen = true;

    private void OnLogin() => _progressScanner.ScanAll(_contentManager.Items);
    private void OnTerritoryChanged(ushort territoryId) => _progressScanner.ScanZone(_contentManager.Items);
}
```

- [ ] **Step 4: Create Configuration.cs**

```csharp
using Dalamud.Configuration;
using System.Numerics;

namespace FfxivTodo;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public Vector2 OverlayPosition { get; set; } = new(100, 100);
    public float OverlayWidth { get; set; } = 300;
    public float OverlayOpacity { get; set; } = 80;
    public int OverlayMaxItems { get; set; } = 10;
    public bool OverlayLocked { get; set; } = false;
    public float OverlayFontScale { get; set; } = 1.0f;
    public bool ShowIgnored { get; set; } = false;
    public int ScanDebounceMs { get; set; } = 2000;
}
```

- [ ] **Step 5: Build to verify project compiles**

Run: `dotnet build FfxivTodo/FfxivTodo.csproj`
Expected: Build succeeds (may need Dalamud SDK installed; ignore dependency warnings for now)

- [ ] **Step 6: Commit**

```bash
git add FfxivTodo/
git commit -m "feat: scaffold project structure with Plugin, Configuration, and manifest"
```

---

### Task 2: Data Models

**Files:**
- Create: `FfxivTodo/Models/Enums.cs`
- Create: `FfxivTodo/Models/ContentItem.cs`
- Create: `FfxivTodo/Models/ProgressEntry.cs`

- [ ] **Step 1: Create Enums.cs**

```csharp
using System.Text.Json.Serialization;

namespace FfxivTodo.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Expansion
{
    ARR,
    HW,
    SB,
    ShB,
    EW,
    DT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentCategory
{
    SideQuest,
    BlueUnlock,
    JobQuest,
    RoleQuest,
    TrialSeries,
    RaidSeries,
    AllianceRaid,
    BeastTribe,
    CustomDelivery
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ItemStatus
{
    NotStarted,
    InProgress,
    Completed
}
```

- [ ] **Step 2: Create ContentItem.cs**

```csharp
using System.Text.Json.Serialization;

namespace FfxivTodo.Models;

public sealed class ContentItem
{
    [JsonPropertyName("id")]
    public uint Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("level")]
    public uint Level { get; init; }

    [JsonPropertyName("expansion")]
    public Expansion Expansion { get; init; }

    [JsonPropertyName("category")]
    public ContentCategory Category { get; init; }

    [JsonPropertyName("prerequisiteIds")]
    public uint[] PrerequisiteIds { get; init; } = [];

    [JsonPropertyName("locationTerritoryId")]
    public uint? LocationTerritoryId { get; init; }

    [JsonPropertyName("locationMapX")]
    public float? LocationMapX { get; init; }

    [JsonPropertyName("locationMapY")]
    public float? LocationMapY { get; init; }

    [JsonPropertyName("questId")]
    public uint? QuestId { get; init; }

    [JsonPropertyName("achievementId")]
    public uint? AchievementId { get; init; }

    [JsonPropertyName("wikiUrl")]
    public string? WikiUrl { get; init; }
}
```

- [ ] **Step 3: Create ProgressEntry.cs**

```csharp
using System.Text.Json.Serialization;

namespace FfxivTodo.Models;

public sealed class ProgressEntry
{
    [JsonPropertyName("status")]
    public ItemStatus Status { get; set; } = ItemStatus.NotStarted;

    [JsonPropertyName("isTracked")]
    public bool IsTracked { get; set; }

    [JsonPropertyName("isIgnored")]
    public bool IsIgnored { get; set; }

    [JsonPropertyName("isManual")]
    public bool IsManual { get; set; }
}
```

- [ ] **Step 4: Commit**

```bash
git add FfxivTodo/Models/
git commit -m "feat: add data models (ContentItem, ProgressEntry, enums)"
```

---

### Task 3: ContentManager

**Files:**
- Create: `FfxivTodo/Services/ContentManager.cs`

- [ ] **Step 1: Create ContentManager.cs**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FfxivTodo.Models;

namespace FfxivTodo.Services;

public sealed class ContentManager
{
    public IReadOnlyList<ContentItem> Items { get; private set; } = [];

    private readonly Dictionary<uint, ContentItem> _itemMap = new();
    private readonly Dictionary<uint, ProgressEntry> _progress = new();

    public void LoadContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("FfxivTodo.Data.content.json");
        if (stream == null)
            throw new FileNotFoundException("content.json not found as embedded resource");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var data = JsonSerializer.Deserialize<ContentDb>(json);
        Items = data?.Items ?? [];

        _itemMap.Clear();
        foreach (var item in Items)
            _itemMap[item.Id] = item;
    }

    public void SetProgress(Dictionary<uint, ProgressEntry> progress)
    {
        _progress.Clear();
        foreach (var kvp in progress)
            _progress[kvp.Key] = kvp.Value;
    }

    public bool IsLocked(uint itemId)
    {
        if (!_itemMap.TryGetValue(itemId, out var item))
            return true;

        return item.PrerequisiteIds.Any(prereqId =>
            !_progress.TryGetValue(prereqId, out var entry) ||
            entry.Status != ItemStatus.Completed);
    }

    public IReadOnlyList<ContentItem> GetPrerequisites(uint itemId)
    {
        if (!_itemMap.TryGetValue(itemId, out var item))
            return [];

        return item.PrerequisiteIds
            .Where(id => _itemMap.ContainsKey(id))
            .Select(id => _itemMap[id])
            .ToList();
    }

    public IReadOnlyList<ContentItem> GetChildren(Expansion expansion, ContentCategory? category = null)
    {
        return Items
            .Where(i => i.Expansion == expansion)
            .Where(i => category == null || i.Category == category)
            .ToList();
    }

    public IReadOnlyList<IGrouping<ContentCategory, ContentItem>> GetGroupedByCategory(Expansion expansion)
    {
        return Items
            .Where(i => i.Expansion == expansion)
            .GroupBy(i => i.Category)
            .ToList();
    }

    public IReadOnlyList<IGrouping<Expansion, ContentItem>> GetGroupedByExpansion()
    {
        return Items
            .GroupBy(i => i.Expansion)
            .OrderBy(g => g.Key)
            .ToList();
    }

    private sealed class ContentDb
    {
        public int Version { get; set; }
        public ContentItem[] Items { get; set; } = [];
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Services/ContentManager.cs
git commit -m "feat: add ContentManager to load content.json and compute prerequisite state"
```

---

### Task 4: ProgressStore

**Files:**
- Create: `FfxivTodo/Services/ProgressStore.cs`

- [ ] **Step 1: Create ProgressStore.cs**

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Services/ProgressStore.cs
git commit -m "feat: add ProgressStore for loading/saving player progress"
```

---

### Task 5: ProgressScanner

**Files:**
- Create: `FfxivTodo/Services/ProgressScanner.cs`

- [ ] **Step 1: Create ProgressScanner.cs**

```csharp
using System.Collections.Generic;
using System.Threading;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Services/ProgressScanner.cs
git commit -m "feat: add ProgressScanner for auto-detecting completion from journal and achievements"
```

---

### Task 6: MainWindow

**Files:**
- Create: `FfxivTodo/Windows/MainWindow.cs`

- [ ] **Step 1: Create MainWindow.cs**

```csharp
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
                var count = catGroup.Count();

                if (!ImGui.TreeNodeEx($"{catGroup.Key} ({count})##cat", ImGuiTreeNodeFlags.DefaultOpen))
                    continue;

                foreach (var item in filteredItems)
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
        var displayName = entry.Ignored ? $"(ignored) {item.Name}" : item.Name;
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
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Windows/MainWindow.cs
git commit -m "feat: add MainWindow with tree view, filters, detail panel, and context menus"
```

---

### Task 7: OverlayWindow

**Files:**
- Create: `FfxivTodo/Windows/OverlayWindow.cs`

- [ ] **Step 1: Create OverlayWindow.cs**

```csharp
using System;
using System.Diagnostics;
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
                if (ImGui.MenuItem("Flag on Map"))
                    _mapFlagHelper.PlaceFlag(item);
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
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Windows/OverlayWindow.cs
git commit -m "feat: add OverlayWindow for tracked items with configurable position and opacity"
```

---

### Task 8: MapFlagHelper

**Files:**
- Create: `FfxivTodo/Services/MapFlagHelper.cs`

- [ ] **Step 1: Create MapFlagHelper.cs**

```csharp
using Dalamud.Game.Text;
using FfxivTodo.Models;

namespace FfxivTodo.Services;

public sealed class MapFlagHelper
{
    public void PlaceFlag(ContentItem item)
    {
        if (!item.LocationTerritoryId.HasValue || item.LocationTerritoryId == 0)
            return;
        if (!item.LocationMapX.HasValue || !item.LocationMapY.HasValue)
            return;

        var territoryId = item.LocationTerritoryId.Value;
        var mapX = item.LocationMapX.Value;
        var mapY = item.LocationMapY.Value;

        // Convert 0-100 coordinates to Dalamud's map coordinate space
        var adjustedX = ConvertCoordinate(mapX);
        var adjustedY = ConvertCoordinate(mapY);

        // Open the map centered on the flag location
        Plugin.GameGui.OpenMapWithMapLink(
            territoryId,
            adjustedX,
            adjustedY
        );
    }

    private static float ConvertCoordinate(float coord)
    {
        // Dalamud uses tile-scale coordinates.
        // FFXIV map coordinates (0-100) need to be scaled for each territory.
        // v1: use raw values; territory-specific scaling is out of scope.
        return coord;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Services/MapFlagHelper.cs
git commit -m "feat: add MapFlagHelper to place in-game map flags"
```

---

### Task 9: Wire Plugin Integration

**Files:**
- Modify: `FfxivTodo/Plugin.cs` — add MapFlagHelper, wire windows, load data on startup

- [ ] **Step 1: Update Plugin.cs to wire MapFlagHelper and load data

Replace the Plugin constructor and related methods with:

```csharp
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "FFXIV Todo";

    [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IQuestManager QuestManager { get; private set; } = null!;
    [PluginService] internal static IAchievementManager AchievementManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FfxivTodo");

    private readonly ContentManager _contentManager;
    private readonly ProgressStore _progressStore;
    private readonly ProgressScanner _progressScanner;
    private readonly MapFlagHelper _mapFlagHelper;
    private readonly MainWindow _mainWindow;
    private readonly OverlayWindow _overlayWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _mapFlagHelper = new MapFlagHelper();
        _contentManager = new ContentManager();
        _progressStore = new ProgressStore(PluginInterface.ConfigDirectory.FullName);
        _progressScanner = new ProgressScanner(_progressStore);
        _progressScanner.SetDebounce(Configuration.ScanDebounceMs);

        _contentManager.LoadContent();
        _progressStore.Load();
        _contentManager.SetProgress(_progressStore.GetAll());

        _mainWindow = new MainWindow(_contentManager, _progressStore, _progressScanner, _mapFlagHelper);
        _overlayWindow = new OverlayWindow(_contentManager, _progressStore, _mapFlagHelper);
        _overlayWindow.IsOpen = false;

        WindowSystem.AddWindow(_mainWindow);
        WindowSystem.AddWindow(_overlayWindow);

        CommandManager.AddHandler("/todo", new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle FFXIV Todo windows"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        PluginInterface.UiBuilder.SaveConfig += OnSaveConfig;
        ClientState.Login += OnLogin;
        ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler("/todo");
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        PluginInterface.UiBuilder.SaveConfig -= OnSaveConfig;
        ClientState.Login -= OnLogin;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        _progressScanner.Dispose();
        _mainWindow.Dispose();
        _overlayWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args == "overlay")
            _overlayWindow.IsOpen = !_overlayWindow.IsOpen;
        else if (args == "refresh")
            OnRefresh();
        else
            _mainWindow.IsOpen = !_mainWindow.IsOpen;
    }

    private void DrawUI() => WindowSystem.Draw();

    private void DrawConfigUI() => _mainWindow.IsOpen = true;

    private void OnSaveConfig()
    {
        PluginInterface.SavePluginConfig(Configuration);
        _progressScanner.SetDebounce(Configuration.ScanDebounceMs);
    }

    private void OnLogin() => OnRefresh();

    private void OnTerritoryChanged(ushort territoryId) => _progressScanner.ScanZone(_contentManager.Items);

    private void OnRefresh()
    {
        _progressStore.Load();
        _progressScanner.ScanAll(_contentManager.Items);
        _contentManager.SetProgress(_progressStore.GetAll());
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Plugin.cs
git commit -m "feat: wire Plugin integration — MapFlagHelper, data loading, all commands"
```

---

### Task 10: Sample Content Data

**Files:**
- Create: `FfxivTodo/Data/content.json`

- [ ] **Step 1: Create content.json with sample entries covering each category

```json
{
  "version": 1,
  "items": [
    {
      "id": 1,
      "name": "The Binding Coil of Bahamut - Turn 1",
      "level": 50,
      "expansion": "ARR",
      "category": "RaidSeries",
      "prerequisiteIds": [],
      "locationTerritoryId": 103,
      "locationMapX": 20.8,
      "locationMapY": 26.2,
      "questId": null,
      "achievementId": 690,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/The_Binding_Coil_of_Bahamut_-_Turn_1"
    },
    {
      "id": 2,
      "name": "A Pup No Longer",
      "level": 30,
      "expansion": "ARR",
      "category": "JobQuest",
      "prerequisiteIds": [],
      "locationTerritoryId": 102,
      "locationMapX": 15.0,
      "locationMapY": 11.5,
      "questId": 66628,
      "achievementId": null,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/A_Pup_No_Longer"
    },
    {
      "id": 3,
      "name": "Beast Tribe Quests - Amalj'aa",
      "level": 43,
      "expansion": "ARR",
      "category": "BeastTribe",
      "prerequisiteIds": [2],
      "locationTerritoryId": 146,
      "locationMapX": 23.0,
      "locationMapY": 14.2,
      "questId": 66182,
      "achievementId": null,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Beast_Tribe_Quests"
    },
    {
      "id": 4,
      "name": "The Howling Eye (Extreme)",
      "level": 50,
      "expansion": "ARR",
      "category": "TrialSeries",
      "prerequisiteIds": [],
      "locationTerritoryId": 108,
      "locationMapX": 28.0,
      "locationMapY": 17.0,
      "questId": null,
      "achievementId": 693,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/The_Howling_Eye_(Extreme)"
    },
    {
      "id": 5,
      "name": "The Labyrinth of the Ancients",
      "level": 50,
      "expansion": "ARR",
      "category": "AllianceRaid",
      "prerequisiteIds": [],
      "locationTerritoryId": 150,
      "locationMapX": 31.5,
      "locationMapY": 12.0,
      "questId": 66904,
      "achievementId": 816,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/The_Labyrinth_of_the_Ancients"
    },
    {
      "id": 6,
      "name": "Leves of Camp Dragonhead",
      "level": 40,
      "expansion": "ARR",
      "category": "SideQuest",
      "prerequisiteIds": [],
      "locationTerritoryId": 155,
      "locationMapX": 26.5,
      "locationMapY": 17.0,
      "questId": null,
      "achievementId": null,
      "wikiUrl": null
    },
    {
      "id": 7,
      "name": "Aether Current Quests - The Dravanian Forelands",
      "level": 52,
      "expansion": "HW",
      "category": "BlueUnlock",
      "prerequisiteIds": [],
      "locationTerritoryId": 200,
      "locationMapX": 32.0,
      "locationMapY": 22.0,
      "questId": 67201,
      "achievementId": null,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Aether_Current"
    },
    {
      "id": 8,
      "name": "Custom Deliveries - Zhloe",
      "level": 60,
      "expansion": "HW",
      "category": "CustomDelivery",
      "prerequisiteIds": [],
      "locationTerritoryId": 218,
      "locationMapX": 11.0,
      "locationMapY": 9.8,
      "questId": 67903,
      "achievementId": null,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Custom_Deliveries"
    }
  ]
}
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Data/content.json
git commit -m "feat: add sample content.json with entries for each category"
```
