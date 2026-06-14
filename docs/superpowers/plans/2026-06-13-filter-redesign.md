# Filter Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the main window's single-select enum filters with friendly multi-select expansion/category filters, always-visible state chips, and explicit filtering for locked and ignored items.

**Architecture:** Keep the behavior local to the main window, but extract filter semantics and label mapping into a small pure helper so they can be unit-tested without ImGui. `MainWindow` remains responsible for rendering controls and applying the helper during tree filtering.

**Tech Stack:** C# (.NET 10 / C# 14), Dalamud SDK, ImGui via Dalamud bindings, xUnit

**Spec:** `docs/superpowers/specs/2026-06-13-filter-redesign-design.md`

---

## File Structure

```
ffxiv-todo.slnx                                      # Solution entry; include new test project
FfxivTodo/
  Configuration.cs                                   # Remove obsolete ShowIgnored setting
  Windows/
    MainWindow.cs                                    # Render popup multi-selects, state chips, clear button
    MainWindowFilterLogic.cs                         # Pure helper: labels, summaries, derived state, match rules
FfxivTodo.Tests/
  FfxivTodo.Tests.csproj                             # New xUnit test project for plugin-side pure logic
  Windows/
    MainWindowFilterLogicTests.cs                    # Tests for labels, summaries, derived state, filter matching
```

---

### Task 1: Add a test harness for filter logic

**Files:**
- Create: `FfxivTodo.Tests/FfxivTodo.Tests.csproj`
- Modify: `ffxiv-todo.slnx`
- Create: `FfxivTodo.Tests/Windows/MainWindowFilterLogicTests.cs`

- [ ] **Step 1: Create the plugin-side test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\FfxivTodo\\FfxivTodo.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add the new project to the solution**

Update `ffxiv-todo.slnx` with:

```xml
<Project Path="FfxivTodo.Tests/FfxivTodo.Tests.csproj" />
```

- [ ] **Step 3: Write the first failing tests for friendly labels and empty-selection behavior**

```csharp
using FfxivTodo.Models;
using FfxivTodo.Windows;

namespace FfxivTodo.Tests.Windows;

public sealed class MainWindowFilterLogicTests
{
    [Fact]
    public void GetExpansionLabel_ReturnsFriendlyName()
    {
        Assert.Equal("A Realm Reborn", MainWindowFilterLogic.GetExpansionLabel(Expansion.ARR));
        Assert.Equal("Shadowbringers", MainWindowFilterLogic.GetExpansionLabel(Expansion.ShB));
    }

    [Fact]
    public void MatchesExpansion_WhenExpansionSetEmpty_ReturnsTrue()
    {
        var selected = new HashSet<Expansion>();

        Assert.True(MainWindowFilterLogic.MatchesExpansion(Expansion.EW, selected));
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj --filter MainWindowFilterLogicTests`

Expected: FAIL because `MainWindowFilterLogic` does not exist yet

- [ ] **Step 5: Commit the scaffolding and failing tests**

```bash
git add ffxiv-todo.slnx FfxivTodo.Tests/
git commit -m "test: add harness for main window filter logic"
```

---

### Task 2: Implement pure filter logic with TDD coverage

**Files:**
- Create: `FfxivTodo/Windows/MainWindowFilterLogic.cs`
- Modify: `FfxivTodo.Tests/Windows/MainWindowFilterLogicTests.cs`

- [ ] **Step 1: Extend the test file with failing cases for the full filter semantics**

Add tests for:

```csharp
[Fact]
public void GetCategoryLabel_MapsBlueUnlockToUnlockQuests()
{
    Assert.Equal("Unlock quests", MainWindowFilterLogic.GetCategoryLabel(ContentCategory.BlueUnlock));
}

[Fact]
public void GetSummary_WhenTwoSelections_ReturnsCommaSeparatedLabels()
{
    var selected = new HashSet<Expansion> { Expansion.EW, Expansion.DT };

    Assert.Equal(
        "Endwalker, Dawntrail",
        MainWindowFilterLogic.GetSummary(
            selected,
            MainWindowFilterLogic.GetExpansionLabel,
            allLabel: "All"));
}

[Fact]
public void GetDisplayState_IgnoredTakesPrecedenceOverLocked()
{
    var entry = new ProgressEntry { Status = ItemStatus.NotStarted, IsIgnored = true };

    Assert.Equal(
        FilterState.Ignored,
        MainWindowFilterLogic.GetDisplayState(entry, isLocked: true));
}

[Fact]
public void GetDisplayState_LockedDoesNotMapToNotStarted()
{
    var entry = new ProgressEntry { Status = ItemStatus.NotStarted, IsIgnored = false };

    Assert.Equal(
        FilterState.Locked,
        MainWindowFilterLogic.GetDisplayState(entry, isLocked: true));
}

[Fact]
public void MatchesStates_WhenNoStatesSelected_ReturnsTrue()
{
    var selected = new HashSet<FilterState>();

    Assert.True(MainWindowFilterLogic.MatchesState(FilterState.Completed, selected));
}

[Fact]
public void MatchesStates_WhenSelected_UsesOrSemantics()
{
    var selected = new HashSet<FilterState> { FilterState.Completed, FilterState.Locked };

    Assert.True(MainWindowFilterLogic.MatchesState(FilterState.Locked, selected));
    Assert.False(MainWindowFilterLogic.MatchesState(FilterState.InProgress, selected));
}
```

- [ ] **Step 2: Run the tests to verify they fail for the expected missing behaviors**

Run: `dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj --filter MainWindowFilterLogicTests`

Expected: FAIL with missing members or failing assertions for unimplemented mapping logic

- [ ] **Step 3: Implement the minimal helper**

Create `FfxivTodo/Windows/MainWindowFilterLogic.cs` with:

```csharp
using FfxivTodo.Models;

namespace FfxivTodo.Windows;

public enum FilterState
{
    NotStarted,
    InProgress,
    Completed,
    Locked,
    Ignored
}

public static class MainWindowFilterLogic
{
    public static string GetExpansionLabel(Expansion expansion) => expansion switch
    {
        Expansion.ARR => "A Realm Reborn",
        Expansion.HW => "Heavensward",
        Expansion.SB => "Stormblood",
        Expansion.ShB => "Shadowbringers",
        Expansion.EW => "Endwalker",
        Expansion.DT => "Dawntrail",
        _ => expansion.ToString()
    };

    public static string GetCategoryLabel(ContentCategory category) => category switch
    {
        ContentCategory.BlueUnlock => "Unlock quests",
        ContentCategory.BeastTribe => "Tribal quests",
        ContentCategory.SideQuest => "Side quests",
        ContentCategory.JobQuest => "Job quests",
        ContentCategory.RoleQuest => "Role quests",
        ContentCategory.TrialSeries => "Trial series",
        ContentCategory.RaidSeries => "Raid series",
        ContentCategory.AllianceRaid => "Alliance raids",
        ContentCategory.CustomDelivery => "Custom deliveries",
        _ => category.ToString()
    };

    public static string GetStateLabel(FilterState state) => state switch
    {
        FilterState.NotStarted => "Not started",
        FilterState.InProgress => "In progress",
        _ => state.ToString()
    };

    public static FilterState GetDisplayState(ProgressEntry entry, bool isLocked)
    {
        if (entry.IsIgnored) return FilterState.Ignored;
        if (isLocked) return FilterState.Locked;

        return entry.Status switch
        {
            ItemStatus.Completed => FilterState.Completed,
            ItemStatus.InProgress => FilterState.InProgress,
            _ => FilterState.NotStarted
        };
    }

    public static bool MatchesExpansion(Expansion expansion, HashSet<Expansion> selected) =>
        selected.Count == 0 || selected.Contains(expansion);

    public static bool MatchesCategory(ContentCategory category, HashSet<ContentCategory> selected) =>
        selected.Count == 0 || selected.Contains(category);

    public static bool MatchesState(FilterState state, HashSet<FilterState> selected) =>
        selected.Count == 0 || selected.Contains(state);

    public static string GetSummary<T>(
        IReadOnlyCollection<T> selected,
        Func<T, string> getLabel,
        string allLabel)
    {
        if (selected.Count == 0)
            return allLabel;

        var labels = selected.Select(getLabel).ToList();
        if (labels.Count <= 2)
            return string.Join(", ", labels);

        return $"{labels.Count} selected";
    }
}
```

- [ ] **Step 4: Run the tests to verify the helper passes**

Run: `dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj --filter MainWindowFilterLogicTests`

Expected: PASS

- [ ] **Step 5: Refactor the tests to cover clear-summary edge cases**

Add a final test for the `3 selected` summary path:

```csharp
[Fact]
public void GetSummary_WhenThreeSelections_ReturnsCountSummary()
{
    var selected = new HashSet<ContentCategory>
    {
        ContentCategory.SideQuest,
        ContentCategory.BlueUnlock,
        ContentCategory.JobQuest
    };

    Assert.Equal(
        "3 selected",
        MainWindowFilterLogic.GetSummary(
            selected,
            MainWindowFilterLogic.GetCategoryLabel,
            allLabel: "All"));
}
```

- [ ] **Step 6: Re-run the tests to keep the suite green**

Run: `dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj --filter MainWindowFilterLogicTests`

Expected: PASS

- [ ] **Step 7: Commit the helper and passing tests**

```bash
git add FfxivTodo/Windows/MainWindowFilterLogic.cs FfxivTodo.Tests/Windows/MainWindowFilterLogicTests.cs
git commit -m "feat: add tested filter logic for main window"
```

---

### Task 3: Replace the main window filter UI and wire in the helper

**Files:**
- Modify: `FfxivTodo/Windows/MainWindow.cs`
- Modify: `FfxivTodo/Configuration.cs`

- [ ] **Step 1: Add a failing integration-style test for full item matching**

Extend `MainWindowFilterLogicTests.cs` with a pure logic test that mirrors the intended UI behavior:

```csharp
[Fact]
public void CombinedFilters_RequireAndAcrossGroups()
{
    var expansions = new HashSet<Expansion> { Expansion.EW, Expansion.DT };
    var categories = new HashSet<ContentCategory> { ContentCategory.BlueUnlock };
    var states = new HashSet<FilterState> { FilterState.Locked };

    Assert.True(MainWindowFilterLogic.MatchesExpansion(Expansion.EW, expansions));
    Assert.True(MainWindowFilterLogic.MatchesCategory(ContentCategory.BlueUnlock, categories));
    Assert.True(MainWindowFilterLogic.MatchesState(FilterState.Locked, states));

    Assert.False(MainWindowFilterLogic.MatchesCategory(ContentCategory.JobQuest, categories));
}
```

- [ ] **Step 2: Run the tests to verify the current helper still supports the integration condition**

Run: `dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj --filter MainWindowFilterLogicTests`

Expected: PASS

- [ ] **Step 3: Replace the old filter fields in `MainWindow`**

Change:

```csharp
private Expansion? _filterExpansion;
private ContentCategory? _filterCategory;
private ItemStatus? _filterStatus;
private bool _showIgnored;
```

to:

```csharp
private readonly HashSet<Expansion> _selectedExpansions = [];
private readonly HashSet<ContentCategory> _selectedCategories = [];
private readonly HashSet<FilterState> _selectedStates = [];
```

- [ ] **Step 4: Remove the obsolete `Show Ignored` configuration property**

Delete from `FfxivTodo/Configuration.cs`:

```csharp
public bool ShowIgnored { get; set; } = false;
```

- [ ] **Step 5: Replace `DrawFilters` with popup multi-selects, state chips, and a clear button**

Implement helpers inside `MainWindow.cs` such as:

```csharp
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
    ImGui.SameLine();
    DrawStateChips();
    ImGui.SameLine();
    ImGui.SetNextItemWidth(150);
    ImGui.InputTextWithHint("##search", "Search...", ref _searchText, 100);
    ImGui.SameLine();
    if (ImGui.Button("Clear filters"))
        ClearFilters();
}
```

and local helper patterns like:

```csharp
private void DrawMultiSelectFilter<T>(
    string label,
    HashSet<T> selected,
    Func<T, string> getLabel) where T : struct, Enum
{
    ImGui.AlignTextToFramePadding();
    ImGui.TextUnformatted(label);
    ImGui.SameLine();

    var summary = MainWindowFilterLogic.GetSummary(selected, getLabel, "All");
    if (ImGui.BeginCombo($"##{label}", summary))
    {
        if (ImGui.Selectable("All", selected.Count == 0))
            selected.Clear();

        foreach (var value in Enum.GetValues<T>())
        {
            var isSelected = selected.Contains(value);
            if (ImGui.Selectable(getLabel(value), isSelected))
            {
                if (!selected.Add(value))
                    selected.Remove(value);
            }
        }

        ImGui.EndCombo();
    }
}
```

- [ ] **Step 6: Add the always-visible state chip renderer**

Use concise labels and toggle styling:

```csharp
private void DrawStateChips()
{
    ImGui.AlignTextToFramePadding();
    ImGui.TextUnformatted("States");
    ImGui.SameLine();

    foreach (var state in Enum.GetValues<FilterState>())
    {
        var selected = _selectedStates.Contains(state);
        if (selected)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.25f, 0.45f, 0.25f, 1f));

        if (ImGui.SmallButton($"{MainWindowFilterLogic.GetStateLabel(state)}##state-{state}"))
        {
            if (!selected)
                _selectedStates.Add(state);
            else
                _selectedStates.Remove(state);
        }

        if (selected)
            ImGui.PopStyleColor();

        ImGui.SameLine();
    }
}
```

Leave spacing cleanup to a small follow-up refactor after behavior works.

- [ ] **Step 7: Update tree filtering to use the helper**

Replace:

```csharp
var expansions = _contentManager.GetGroupedByExpansion()
    .Where(g => _filterExpansion == null || g.Key == _filterExpansion);
```

with:

```csharp
var expansions = _contentManager.GetGroupedByExpansion()
    .Where(g => MainWindowFilterLogic.MatchesExpansion(g.Key, _selectedExpansions));
```

Replace:

```csharp
.Where(g => _filterCategory == null || g.Key == _filterCategory);
```

with:

```csharp
.Where(g => MainWindowFilterLogic.MatchesCategory(g.Key, _selectedCategories));
```

Replace the status / ignored filtering block in `FilterItems` with:

```csharp
var locked = _contentManager.IsLocked(item.Id);
var displayState = MainWindowFilterLogic.GetDisplayState(entry, locked);

if (!MainWindowFilterLogic.MatchesState(displayState, _selectedStates))
    continue;
```

- [ ] **Step 8: Add a clear helper**

```csharp
private void ClearFilters()
{
    _selectedExpansions.Clear();
    _selectedCategories.Clear();
    _selectedStates.Clear();
    _searchText = string.Empty;
}
```

- [ ] **Step 9: Build and test the integrated change**

Run:

```bash
dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj
dotnet build FfxivTodo/FfxivTodo.csproj
```

Expected:
- test project passes
- plugin project builds successfully

- [ ] **Step 10: Manually verify the UI in-game**

Check:
- expansion popup supports multi-select
- category popup uses friendly labels
- state chips can isolate `Completed`, `Locked`, and `Ignored`
- `Clear filters` resets all inputs
- locked items are excluded when only `Not started` is selected

- [ ] **Step 11: Commit the UI integration**

```bash
git add FfxivTodo/Windows/MainWindow.cs FfxivTodo/Configuration.cs
git commit -m "feat: redesign main window filters"
```

---

### Task 4: Final cleanup and regression check

**Files:**
- Modify: `FfxivTodo/Windows/MainWindow.cs` (only if needed for spacing/readability)
- Modify: `FfxivTodo.Tests/Windows/MainWindowFilterLogicTests.cs` (only if a missing regression case is discovered)

- [ ] **Step 1: Inspect for dead code from the old enum combo flow**

Remove if unused:

```csharp
private static void DrawEnumCombo<T>(string label, ref T? value) where T : struct, Enum
```

and any leftover references to `_filterExpansion`, `_filterCategory`, `_filterStatus`, or `_showIgnored`.

- [ ] **Step 2: Run a focused search to confirm the old filter path is gone**

Run:

```bash
rg -n "_filterExpansion|_filterCategory|_filterStatus|_showIgnored|ShowIgnored|DrawEnumCombo" FfxivTodo
```

Expected: no matches

- [ ] **Step 3: Re-run the full verification commands**

Run:

```bash
dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj
dotnet build ffxiv-todo.slnx
```

Expected:
- `FfxivTodo.Tests` passes
- `DataBuilder.Tests` still passes
- solution build succeeds

- [ ] **Step 4: Commit the cleanup if anything changed**

```bash
git add FfxivTodo/Windows/MainWindow.cs FfxivTodo.Tests/Windows/MainWindowFilterLogicTests.cs
git commit -m "refactor: remove old main window filter code"
```

