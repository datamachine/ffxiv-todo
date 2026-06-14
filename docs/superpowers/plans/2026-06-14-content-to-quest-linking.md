# Content-to-Quest Linking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Link content items (raid series, trial series, alliance raids) to their unlock quest chains so users can see and complete the quests needed to unlock each content item.

**Architecture:** Add `UnlockQuestIds` field across the data pipeline models and `ContentItem`. A new `UnlockQuestResolver` pipeline stage populates this field using a manual override file plus wiki scraping fallback. The UI renders quest chains as collapsible groups under their parent content. `ProgressStore.SetStatus` detects chain completion and auto-completes the parent.

**Tech Stack:** C# / .NET 10, CsvHelper, AngleSharp (wiki scraping), ImGui (Dalamud), xUnit

**Spec:** `docs/superpowers/specs/2026-06-14-content-to-quest-linking-design.md`

---

## File Structure

| Action | File | Responsibility |
|--------|------|----------------|
| Modify | `FfxivTodo/Models/ContentItem.cs` | Add `UnlockQuestIds` field |
| Modify | `DataBuilder/Models/PipelineModels.cs` | Add `UnlockQuestIds` to `DetailItem` and `FormattedItem` |
| Modify | `DataBuilder/Data/CsvDataProvider.cs` | Add `LookupQuestById` method |
| Create | `DataBuilder/Data/quest_chain_overrides.json` | Manual override mapping |
| Create | `DataBuilder/Scrapers/UnlockQuestResolver.cs` | New pipeline stage |
| Modify | `DataBuilder/Formatters/ContentJsonFormatter.cs` | Map `UnlockQuestIds` through formatting |
| Modify | `DataBuilder/Program.cs` | Wire new stage into pipeline |
| Modify | `FfxivTodo/Services/ContentManager.cs` | Add quest chain lookup methods |
| Modify | `FfxivTodo/Services/ProgressStore.cs` | Extend `SetStatus` with chain auto-completion |
| Modify | `FfxivTodo/Windows/MainWindow.cs` | Quest chain group rendering, next-quest indicator |

---

### Task 1: Add `UnlockQuestIds` to Pipeline Models

**Files:**
- Modify: `DataBuilder/Models/PipelineModels.cs`
- Test: `DataBuilder.Tests/Formatters/ContentJsonFormatterTests.cs`

- [ ] **Step 1: Write the failing test**

Add a test verifying that `ContentJsonFormatter.Format` maps `UnlockQuestIds` from `DetailItem` to `FormattedItem`:

```csharp
[Fact]
public void Format_UnlockQuestIds_MappedToFormattedItem()
{
    var items = new List<DetailItem>
    {
        new()
        {
            Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB",
            Level = 80, UnlockQuestIds = [69163]
        }
    };

    var result = ContentJsonFormatter.Format(items);

    var item = Assert.Single(result.Items);
    Assert.Equal([69163u], item.UnlockQuestIds);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DataBuilder.Tests --filter "Format_UnlockQuestIds_MappedToFormattedItem" --no-restore -v n`
Expected: FAIL — `DetailItem` does not have `UnlockQuestIds`

- [ ] **Step 3: Add `UnlockQuestIds` to `DetailItem`**

In `DataBuilder/Models/PipelineModels.cs`, add to `DetailItem` after `AchievementId`:

```csharp
[JsonPropertyName("unlockQuestIds")]
public List<uint> UnlockQuestIds { get; set; } = new();
```

- [ ] **Step 4: Add `UnlockQuestIds` to `FormattedItem`**

In the same file, add to `FormattedItem` after `AchievementId`:

```csharp
[JsonPropertyName("unlockQuestIds")]
public List<uint> UnlockQuestIds { get; set; } = new();
```

- [ ] **Step 5: Map `UnlockQuestIds` in `ContentJsonFormatter.Format`**

In `DataBuilder/Formatters/ContentJsonFormatter.cs`, in the `foreach` loop creating `FormattedItem` objects (~line 119-133), add after `AchievementId`:

```csharp
UnlockQuestIds = item.UnlockQuestIds,
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test DataBuilder.Tests --filter "Format_UnlockQuestIds_MappedToFormattedItem" --no-restore -v n`
Expected: PASS

- [ ] **Step 7: Run all DataBuilder tests**

Run: `dotnet test DataBuilder.Tests --no-restore -v n`
Expected: All PASS

- [ ] **Step 8: Commit**

```bash
git add DataBuilder/Models/PipelineModels.cs DataBuilder/Formatters/ContentJsonFormatter.cs DataBuilder.Tests/Formatters/ContentJsonFormatterTests.cs
git commit -m "feat: add UnlockQuestIds to pipeline models and formatter"
```

---

### Task 2: Add `UnlockQuestIds` to `ContentItem` (Plugin Model)

**Files:**
- Modify: `FfxivTodo/Models/ContentItem.cs`

- [ ] **Step 1: Add field to `ContentItem`**

In `FfxivTodo/Models/ContentItem.cs`, add after `AchievementId`:

```csharp
public uint[] UnlockQuestIds { get; set; } = [];
```

- [ ] **Step 2: Verify plugin builds**

Run: `dotnet build FfxivTodo --no-restore -v q`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add FfxivTodo/Models/ContentItem.cs
git commit -m "feat: add UnlockQuestIds to ContentItem model"
```

---

### Task 3: Add `LookupQuestById` to `CsvDataProvider`

**Files:**
- Modify: `DataBuilder/Data/CsvDataProvider.cs`
- Test: `DataBuilder.Tests/Data/CsvDataProviderTests.cs`

- [ ] **Step 1: Write the failing test**

Add a test verifying lookup by ID returns the full quest row:

```csharp
[Fact]
public async Task LookupQuestById_ExistingId_ReturnsRow()
{
    var provider = await CreateProviderAsync();
    var quest = provider.LookupQuest("Hallo Halatali");
    Assert.NotNull(quest);

    var byId = provider.LookupQuestById(quest.Id);
    Assert.NotNull(byId);
    Assert.Equal(quest.Name, byId.Name);
}

[Fact]
public async Task LookupQuestById_NonexistentId_ReturnsNull()
{
    var provider = await CreateProviderAsync();
    var result = provider.LookupQuestById(999999);
    Assert.Null(result);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DataBuilder.Tests --filter "LookupQuestById" --no-restore -v n`
Expected: FAIL — method does not exist

- [ ] **Step 3: Implement `LookupQuestById`**

In `DataBuilder/Data/CsvDataProvider.cs`, add after the `LookupQuest` method (~line 98):

```csharp
public QuestCsvRow? LookupQuestById(int questId)
{
    return _questById.TryGetValue(questId, out var row) ? row : null;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DataBuilder.Tests --filter "LookupQuestById" --no-restore -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add DataBuilder/Data/CsvDataProvider.cs DataBuilder.Tests/Data/CsvDataProviderTests.cs
git commit -m "feat: add LookupQuestById to CsvDataProvider"
```

---

### Task 4: Create Quest Chain Override File

**Files:**
- Create: `DataBuilder/Data/quest_chain_overrides.json`
- Create: `DataBuilder/Models/QuestChainOverride.cs` (model for deserialization)

- [ ] **Step 1: Create the override model**

Create `DataBuilder/Models/QuestChainOverride.cs`:

```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataBuilder.Models;

public sealed class QuestChainOverride
{
    [JsonPropertyName("contentId")]
    public uint ContentId { get; set; }

    [JsonPropertyName("questIds")]
    public List<uint> QuestIds { get; set; } = new();
}

public sealed class QuestChainOverridesFile
{
    [JsonPropertyName("overrides")]
    public List<QuestChainOverride> Overrides { get; set; } = new();
}
```

- [ ] **Step 2: Create the override file**

Create `DataBuilder/Data/quest_chain_overrides.json`:

```json
{
  "overrides": []
}
```

- [ ] **Step 3: Commit**

```bash
git add DataBuilder/Models/QuestChainOverride.cs DataBuilder/Data/quest_chain_overrides.json
git commit -m "feat: add quest chain override model and empty overrides file"
```

---

### Task 5: Implement `UnlockQuestResolver` — Override Loading

**Spec deviation note:** The spec uses `contentId` in the override file, but content item IDs are only assigned at formatting time (Stage 4). The plan uses `contentName` instead — this is stable, human-readable, and matches the existing `achievement_overrides.json` pattern.

**Files:**
- Create: `DataBuilder/Scrapers/UnlockQuestResolver.cs`
- Test: `DataBuilder.Tests/Scrapers/UnlockQuestResolverTests.cs`
- Modify: `DataBuilder/Models/QuestChainOverride.cs` (update from Task 4)

- [ ] **Step 1: Update `QuestChainOverride` model to use `contentName`**

Change `DataBuilder/Models/QuestChainOverride.cs` from `ContentId` to `ContentName`:

```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataBuilder.Models;

public sealed class QuestChainOverride
{
    [JsonPropertyName("contentName")]
    public string ContentName { get; set; } = string.Empty;

    [JsonPropertyName("questIds")]
    public List<uint> QuestIds { get; set; } = new();
}

public sealed class QuestChainOverridesFile
{
    [JsonPropertyName("overrides")]
    public List<QuestChainOverride> Overrides { get; set; } = new();
}
```

Update `DataBuilder/Data/quest_chain_overrides.json`:

```json
{
  "overrides": [
    { "contentName": "Eden's Gate", "questIds": [69163] }
  ]
}
```

- [ ] **Step 2: Write the failing tests for override loading**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DataBuilder.Models;
using DataBuilder.Scrapers;

namespace DataBuilder.Tests.Scrapers;

public sealed class UnlockQuestResolverTests
{
    [Fact]
    public void Resolve_WithOverride_PopulatesUnlockQuestIds()
    {
        var overrides = new QuestChainOverridesFile
        {
            Overrides =
            [
                new() { ContentName = "Eden's Gate", QuestIds = [69163] }
            ]
        };

        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

            var items = new List<DetailItem>
            {
                new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            resolver.Resolve(items);

            Assert.Equal([69163u], items[0].UnlockQuestIds);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void Resolve_WithoutOverride_DoesNotModifyItem()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(new QuestChainOverridesFile()));

            var items = new List<DetailItem>
            {
                new() { Name = "Some Item", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            resolver.Resolve(items);

            Assert.Empty(items[0].UnlockQuestIds);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void Resolve_WithOverride_SkipsWikiScrapeForOverriddenItem()
    {
        var overrides = new QuestChainOverridesFile
        {
            Overrides =
            [
                new() { ContentName = "Eden's Gate", QuestIds = [69163] }
            ]
        };

        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

            var items = new List<DetailItem>
            {
                new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            resolver.Resolve(items);

            Assert.True(resolver.IsOverridden("Eden's Gate"));
            Assert.False(resolver.IsOverridden("Unknown Raid"));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test DataBuilder.Tests --filter "UnlockQuestResolver" --no-restore -v n`
Expected: FAIL — type does not exist

- [ ] **Step 4: Implement `UnlockQuestResolver` with override loading**

Create `DataBuilder/Scrapers/UnlockQuestResolver.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class UnlockQuestResolver
{
    private readonly string _overrideFilePath;
    private Dictionary<string, List<uint>> _overridesByName = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _overriddenNames = new(StringComparer.OrdinalIgnoreCase);

    public UnlockQuestResolver(string overrideFilePath)
    {
        _overrideFilePath = overrideFilePath;
        LoadOverrides();
    }

    public bool IsOverridden(string contentName) => _overriddenNames.Contains(contentName);

    public void Resolve(List<DetailItem> items)
    {
        foreach (var item in items)
        {
            if (!_overridesByName.TryGetValue(item.Name, out var questIds))
                continue;

            item.UnlockQuestIds = new List<uint>(questIds);
        }
    }

    private void LoadOverrides()
    {
        if (!File.Exists(_overrideFilePath))
            return;

        var json = File.ReadAllText(_overrideFilePath);
        var file = JsonSerializer.Deserialize<QuestChainOverridesFile>(json);
        if (file?.Overrides == null || file.Overrides.Count == 0)
            return;

        _overridesByName = file.Overrides
            .GroupBy(o => o.ContentName)
            .ToDictionary(g => g.Key, g => g.First().QuestIds, StringComparer.OrdinalIgnoreCase);

        _overriddenNames = new HashSet<string>(
            file.Overrides.Select(o => o.ContentName), StringComparer.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test DataBuilder.Tests --filter "UnlockQuestResolver" --no-restore -v n`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add DataBuilder/Scrapers/UnlockQuestResolver.cs DataBuilder.Tests/Scrapers/UnlockQuestResolverTests.cs DataBuilder/Models/QuestChainOverride.cs DataBuilder/Data/quest_chain_overrides.json
git commit -m "feat: add UnlockQuestResolver with override loading"
```

---

### Task 6: Implement `UnlockQuestResolver` — Wiki Scraping Fallback

Per the spec, wiki scraping is the primary source for unlock quest data. The override file takes priority and causes wiki scraping to be skipped for that item. For all other `RaidSeries`, `TrialSeries`, and `AllianceRaid` items without a `QuestId`, we scrape their wiki pages to find unlock quest names, then match them against the Quest CSV.

**Files:**
- Modify: `DataBuilder/Scrapers/UnlockQuestResolver.cs`
- Modify: `DataBuilder.Tests/Scrapers/UnlockQuestResolverTests.cs`
- Create: `DataBuilder.Tests/TestData/edens_gate.html`

- [ ] **Step 1: Create a test fixture for wiki page scraping**

Create `DataBuilder.Tests/TestData/edens_gate.html`:

```html
<!DOCTYPE html>
<html>
<body>
<div id="bodyContent">
<p>Eden's Gate is the first tier of the Eden raid series. It can be unlocked by completing the quest In the Middle of Nowhere.</p>
<p>The raid requires completing the main scenario quest Shadowbringers.</p>
</div>
</body>
</html>
```

- [ ] **Step 2: Write the failing test for wiki scraping**

```csharp
[Fact]
public void Resolve_WikiScrape_ExtractsUnlockQuestName()
{
    var wikiHtml = File.ReadAllText(Path.Combine("TestData", "edens_gate.html"));
    var questNames = UnlockQuestResolver.ExtractUnlockQuestNames(wikiHtml);
    Assert.Contains("In the Middle of Nowhere", questNames);
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test DataBuilder.Tests --filter "ExtractUnlockQuestNames" --no-restore -v n`
Expected: FAIL — method does not exist

- [ ] **Step 4: Implement `ExtractUnlockQuestNames`**

Add to `UnlockQuestResolver`:

```csharp
private static readonly Regex[] UnlockQuestPatterns =
[
    new(@"completing the quest (.+?)(?:\.|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    new(@"starting the quest (.+?)(?:\.|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    new(@"the quest (.+?) must be completed", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    new(@"unlocked by completing the quest (.+?)(?:\.|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
];

public static List<string> ExtractUnlockQuestNames(string html)
{
    var names = new List<string>();
    foreach (var pattern in UnlockQuestPatterns)
    {
        foreach (Match match in pattern.Matches(html))
        {
            var name = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
        }
    }
    return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test DataBuilder.Tests --filter "ExtractUnlockQuestNames" --no-restore -v n`
Expected: PASS

- [ ] **Step 6: Add async `ResolveWithWikiAsync` method**

This method takes `CsvDataProvider` and an `HttpClient`, scrapes wiki pages for non-overridden items, resolves quest names to IDs, and builds chains.

```csharp
public async Task ResolveWithWikiAsync(List<DetailItem> items, CsvDataProvider csv, HttpClient http)
{
    Resolve(items);

    var dutyCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "RaidSeries", "TrialSeries", "AllianceRaid"
    };

    var unresolved = items
        .Where(i => dutyCategories.Contains(i.Category))
        .Where(i => i.UnlockQuestIds.Count == 0)
        .Where(i => i.QuestId == null || i.QuestId == 0)
        .Where(i => !IsOverridden(i.Name))
        .ToList();

    foreach (var item in unresolved)
    {
        if (string.IsNullOrEmpty(item.WikiUrl))
            continue;

        try
        {
            var html = await http.GetStringAsync(item.WikiUrl);
            var questNames = ExtractUnlockQuestNames(html);

            foreach (var questName in questNames)
            {
                var questRow = csv.LookupQuest(questName);
                if (questRow != null)
                    item.UnlockQuestIds.Add((uint)questRow.Id);
            }

            if (item.UnlockQuestIds.Count > 0)
                Console.WriteLine($"  Wiki: {item.Name} → {string.Join(", ", questNames)}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  WARN: Failed to scrape {item.WikiUrl}: {ex.Message}");
        }
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add DataBuilder/Scrapers/UnlockQuestResolver.cs DataBuilder.Tests/Scrapers/UnlockQuestResolverTests.cs DataBuilder.Tests/TestData/edens_gate.html
git commit -m "feat: add wiki scraping fallback to UnlockQuestResolver"
```

---

### Task 7: Implement `UnlockQuestResolver` — Quest Chain Walking and Entry Creation

Per the spec, once we have quest IDs (from overrides or wiki scraping), we walk `PreviousQuest0-2` backward through the Quest CSV to discover the full prerequisite chain, then reverse it into dependency order. For any quest in the chain that doesn't already exist as a `DetailItem`, we create one.

**Files:**
- Modify: `DataBuilder/Scrapers/UnlockQuestResolver.cs`
- Modify: `DataBuilder.Tests/Scrapers/UnlockQuestResolverTests.cs`

- [ ] **Step 1: Write the failing test for chain walking**

```csharp
[Fact]
public void Resolve_WithOverride_CreatesMissingQuestEntries()
{
    var overrides = new QuestChainOverridesFile
    {
        Overrides =
        [
            new() { ContentName = "Eden's Gate", QuestIds = [69163] }
        ]
    };

    var tmpFile = Path.GetTempFileName();
    try
    {
        File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

        var items = new List<DetailItem>
        {
            new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
        };

        var resolver = new UnlockQuestResolver(tmpFile);
        var newItems = resolver.ResolveWithChainCreation(items, csv: null);

        Assert.Equal(2, items.Count);
        Assert.Equal("Eden's Gate", items[0].Name);
        Assert.Equal([69163u], items[0].UnlockQuestIds);
        Assert.Equal("BlueUnlock", items[1].Category);
    }
    finally
    {
        File.Delete(tmpFile);
    }
}

[Fact]
public void Resolve_WithOverride_WalksPrerequisiteChain()
{
    var overrides = new QuestChainOverridesFile
    {
        Overrides =
        [
            new() { ContentName = "Eden's Gate", QuestIds = [69163] }
        ]
    };

    var tmpFile = Path.GetTempFileName();
    try
    {
        File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

        var items = new List<DetailItem>
        {
            new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
        };

        var resolver = new UnlockQuestResolver(tmpFile);
        var newItems = resolver.ResolveWithChainCreation(items, csv: null);

        // Without CSV, the chain is just the override quest IDs — no backward walking possible
        Assert.Equal([69163u], items[0].UnlockQuestIds);
        // A new entry was created for quest 69163
        Assert.Single(newItems);
        Assert.Equal(69163u, newItems[0].QuestId);
    }
    finally
    {
        File.Delete(tmpFile);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DataBuilder.Tests --filter "Resolve_WithOverride_CreatesMissingQuestEntries" --no-restore -v n`
Expected: FAIL — method does not exist

- [ ] **Step 3: Implement `ResolveWithChainCreation`**

Add to `UnlockQuestResolver`:

```csharp
private static readonly string[] ExpansionNames = ["ARR", "HW", "SB", "ShB", "EW", "DT"];

public List<DetailItem> ResolveWithChainCreation(List<DetailItem> items, CsvDataProvider? csv)
{
    Resolve(items);

    var newItems = new List<DetailItem>();

    var existingQuestIds = new HashSet<uint>();
    foreach (var item in items)
    {
        if (item.QuestId.HasValue && item.QuestId.Value > 0)
            existingQuestIds.Add(item.QuestId.Value);
    }

    var chainsToAdd = new Dictionary<uint, List<uint>>();

    foreach (var item in items)
    {
        if (item.UnlockQuestIds.Count == 0)
            continue;

        var fullChain = new List<uint>();
        foreach (var terminalQuestId in item.UnlockQuestIds)
        {
            if (csv != null)
            {
                var walked = WalkPrerequisiteChain(terminalQuestId, csv, existingQuestIds);
                fullChain.AddRange(walked);
            }
            else
            {
                if (!existingQuestIds.Contains(terminalQuestId))
                    fullChain.Add(terminalQuestId);
            }
        }

        if (fullChain.Count > 0 && csv != null)
        {
            item.UnlockQuestIds = fullChain;
        }

        foreach (var questId in item.UnlockQuestIds)
        {
            if (existingQuestIds.Contains(questId))
                continue;

            var questRow = csv?.LookupQuestById((int)questId);
            var questName = questRow?.Name ?? $"Quest {questId}";

            var newItem = new DetailItem
            {
                Name = questName,
                Category = "BlueUnlock",
                Expansion = questRow != null && questRow.Expansion >= 0 && questRow.Expansion < ExpansionNames.Length
                    ? ExpansionNames[questRow.Expansion]
                    : item.Expansion,
                Level = questRow?.ClassJobLevel > 0 ? (uint?)questRow.ClassJobLevel : null,
                QuestId = questId,
                WikiUrl = $"https://ffxiv.consolegameswiki.com/wiki/{questName.Replace(' ', '_')}",
            };

            if (questRow != null)
            {
                foreach (var pqId in new[] { questRow.PreviousQuest0, questRow.PreviousQuest1, questRow.PreviousQuest2 })
                {
                    if (pqId > 0)
                    {
                        var pqName = csv?.LookupQuestById(pqId)?.Name;
                        if (pqName != null)
                            newItem.PrerequisiteNames.Add(pqName);
                    }
                }
            }

            newItems.Add(newItem);
            items.Add(newItem);
            existingQuestIds.Add(questId);
        }
    }

    return newItems;
}

private static List<uint> WalkPrerequisiteChain(uint terminalQuestId, CsvDataProvider csv, HashSet<uint> existingQuestIds)
{
    var chain = new List<uint>();
    var visited = new HashSet<uint> { terminalQuestId };
    var stack = new Stack<uint>();
    stack.Push(terminalQuestId);

    while (stack.Count > 0)
    {
        var currentId = stack.Pop();

        if (!existingQuestIds.Contains(currentId))
            chain.Insert(0, currentId);

        var row = csv.LookupQuestById((int)currentId);
        if (row == null)
            continue;

        foreach (var pqId in new[] { row.PreviousQuest0, row.PreviousQuest1, row.PreviousQuest2 })
        {
            if (pqId > 0 && visited.Add((uint)pqId))
                stack.Push((uint)pqId);
        }
    }

    return chain;
}
```

The `WalkPrerequisiteChain` method walks backward through `PreviousQuest0-2` starting from the terminal quest ID, collects all prerequisite quest IDs, and returns them in dependency order (first quest first, terminal quest last). It avoids cycles via a `visited` set.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DataBuilder.Tests --filter "UnlockQuestResolver" --no-restore -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add DataBuilder/Scrapers/UnlockQuestResolver.cs DataBuilder.Tests/Scrapers/UnlockQuestResolverTests.cs
git commit -m "feat: add quest chain walking and entry creation to UnlockQuestResolver"
```

---

### Task 8: Wire `UnlockQuestResolver` into the Pipeline

**Files:**
- Modify: `DataBuilder/Program.cs`

This task wires the new `UnlockQuestResolver` stage into the DataBuilder pipeline, after CSV enrichment (Stage 2) and before wiki detail scraping (Stage 3). It uses both the override file and wiki scraping fallback.

- [ ] **Step 1: Add Stage 2.5 to `Program.cs`**

In `DataBuilder/Program.cs`, after the CSV enrichment block (Stage 2, ~line 87) and before Stage 3, add:

```csharp
// Stage 2.5: Resolve unlock quest chains
if (fromStage is "scratch" or "categories")
{
    Console.WriteLine("Stage 2.5: Resolving unlock quest chains...");
    var overridePath = Path.Combine("DataBuilder", "Data", "quest_chain_overrides.json");
    if (!File.Exists(overridePath))
        overridePath = Path.Combine("..", overridePath);
    if (!File.Exists(overridePath))
        Console.Error.WriteLine("  WARN: quest_chain_overrides.json not found");

    var resolver = new UnlockQuestResolver(overridePath);

    await resolver.ResolveWithWikiAsync(detailItems, csvProvider, http);

    var newQuestItems = resolver.ResolveWithChainCreation(detailItems, csvProvider);
    Console.WriteLine($"  Created {newQuestItems.Count} quest chain entries.");

    if (detailFileWritten)
    {
        await File.WriteAllTextAsync(detailFile, JsonSerializer.Serialize(
            new DetailItemsFile { Items = detailItems }, JsonOpts));
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build DataBuilder --no-restore -v q`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add DataBuilder/Program.cs
git commit -m "feat: wire UnlockQuestResolver into pipeline with wiki scraping and chain walking"
```

---

### Task 9: Populate the Override File with Known Quest Chains

The override file covers edge cases where wiki scraping fails or returns incorrect data. For most raid/trial/alliance content, wiki scraping will discover the unlock quest automatically. The override file only needs entries for items where:
- The wiki page doesn't contain the quest name in a parseable format
- The wiki scraping returns the wrong quest
- The content name is ambiguous

**Files:**
- Modify: `DataBuilder/Data/quest_chain_overrides.json`

- [ ] **Step 1: Run the pipeline first with wiki scraping**

Run: `dotnet run --project DataBuilder -- --from categories`
Review the output for items where wiki scraping succeeded vs. failed.

- [ ] **Step 2: Add override entries for items that wiki scraping missed**

For any `RaidSeries`, `TrialSeries`, or `AllianceRaid` item that still has `unlockQuestIds: []` after the pipeline run, research the unlock quest and add it to the override file. Example:

```json
{
  "overrides": [
    { "contentName": "Eden's Gate", "questIds": [69163] }
  ]
}
```

Note: Quest IDs can be verified by running the pipeline with `--from categories` and checking the console output, or by searching the Quest CSV directly.

- [ ] **Step 3: Run the pipeline again to verify**

Run: `dotnet run --project DataBuilder -- --from categories`
Expected: Pipeline completes, `content.json` contains `unlockQuestIds` on all raid/trial/alliance items

- [ ] **Step 4: Commit**

```bash
git add DataBuilder/Data/quest_chain_overrides.json FfxivTodo/Data/content.json
git commit -m "feat: populate quest chain overrides for wiki scraping gaps"
```

---

### Task 10: Add Quest Chain Lookup Methods to `ContentManager`

**Files:**
- Modify: `FfxivTodo/Services/ContentManager.cs`
- Test: `FfxivTodo.Tests/Services/ContentManagerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using FfxivTodo.Models;
using FfxivTodo.Services;

namespace FfxivTodo.Tests.Services;

public sealed class ContentManagerTests
{
    [Fact]
    public void GetUnlockQuests_WithChain_ReturnsQuestItems()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Eden's Gate", Category = ContentCategory.RaidSeries, UnlockQuestIds = [10, 20] },
            new() { Id = 10, Name = "Quest A", Category = ContentCategory.BlueUnlock, QuestId = 100 },
            new() { Id = 20, Name = "Quest B", Category = ContentCategory.BlueUnlock, QuestId = 200 },
        };

        cm.LoadFromList(items);

        var quests = cm.GetUnlockQuests(1);
        Assert.Equal(2, quests.Count);
        Assert.Equal("Quest A", quests[0].Name);
        Assert.Equal("Quest B", quests[1].Name);
    }

    [Fact]
    public void GetUnlockQuests_NoChain_ReturnsEmpty()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Some Item", Category = ContentCategory.SideQuest }
        };

        cm.LoadFromList(items);

        var quests = cm.GetUnlockQuests(1);
        Assert.Empty(quests);
    }

    [Fact]
    public void FindParentContent_QuestInChain_ReturnsParent()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Eden's Gate", Category = ContentCategory.RaidSeries, UnlockQuestIds = [10] },
            new() { Id = 10, Name = "Quest A", Category = ContentCategory.BlueUnlock, QuestId = 100 },
        };

        cm.LoadFromList(items);

        var parent = cm.FindParentContent(10);
        Assert.NotNull(parent);
        Assert.Equal("Eden's Gate", parent.Name);
    }

    [Fact]
    public void FindParentContent_NotInChain_ReturnsNull()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Some Item", Category = ContentCategory.SideQuest }
        };

        cm.LoadFromList(items);

        var parent = cm.FindParentContent(1);
        Assert.Null(parent);
    }
}
```

Note: We need a `LoadFromList` method for testing since `LoadContent` reads from embedded resources.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test FfxivTodo.Tests --filter "ContentManagerTests" --no-restore -v n`
Expected: FAIL — methods don't exist

- [ ] **Step 3: Add `LoadFromList`, `GetUnlockQuests`, and `FindParentContent` to `ContentManager`**

In `FfxivTodo/Services/ContentManager.cs`:

```csharp
private Dictionary<uint, List<uint>> _questIdToParentIds = new();

public void LoadFromList(IReadOnlyList<ContentItem> items)
{
    Items = items;
    _itemMap.Clear();
    foreach (var item in Items)
        _itemMap[item.Id] = item;
    BuildQuestToParentIndex();
}

public void LoadContent()
{
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream("FfxivTodo.Data.content.json");
    if (stream == null)
        throw new FileNotFoundException("content.json not found as embedded resource");

    using var reader = new StreamReader(stream);
    var json = reader.ReadToEnd();
    Plugin.Log.Information($"Read content.json: {json.Length} bytes");
    try
    {
        var data = JsonConvert.DeserializeObject<ContentDb>(json);
        Plugin.Log.Information($"Deserialization result: data={(data != null ? "non-null" : "NULL")}, items={data?.Items?.Length ?? -1}");
        Items = data?.Items ?? [];
        DataVersion = data?.Version ?? 0;
    }
    catch (Exception ex)
    {
        Plugin.Log.Error($"Failed to deserialize content.json: {ex.Message}");
        Plugin.Log.Error($"Exception type: {ex.GetType().FullName}");
        if (ex.InnerException != null)
            Plugin.Log.Error($"Inner: {ex.InnerException.Message}");
        Items = [];
        DataVersion = 0;
    }

    _itemMap.Clear();
    foreach (var item in Items)
        _itemMap[item.Id] = item;
    BuildQuestToParentIndex();
}

public IReadOnlyList<ContentItem> GetUnlockQuests(uint contentId)
{
    if (!_itemMap.TryGetValue(contentId, out var item))
        return [];

    var quests = new List<ContentItem>();
    foreach (var questId in item.UnlockQuestIds)
    {
        var quest = Items.FirstOrDefault(i => i.QuestId == questId);
        if (quest != null)
            quests.Add(quest);
    }

    return quests;
}

public ContentItem? FindParentContent(uint itemId)
{
    if (!_questIdToParentIds.TryGetValue(itemId, out var parentIds))
        return null;

    foreach (var parentId in parentIds)
    {
        if (_itemMap.TryGetValue(parentId, out var parent))
            return parent;
    }

    return null;
}

private void BuildQuestToParentIndex()
{
    _questIdToParentIds = new Dictionary<uint, List<uint>>();
    foreach (var item in Items)
    {
        foreach (var questId in item.UnlockQuestIds)
        {
            if (!_questIdToParentIds.TryGetValue(questId, out var list))
            {
                list = new List<uint>();
                _questIdToParentIds[questId] = list;
            }
            list.Add(item.Id);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test FfxivTodo.Tests --filter "ContentManagerTests" --no-restore -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add FfxivTodo/Services/ContentManager.cs FfxivTodo.Tests/Services/ContentManagerTests.cs
git commit -m "feat: add quest chain lookup methods to ContentManager"
```

---

### Task 11: Extend `ProgressStore.SetStatus` with Chain Auto-Completion

**Files:**
- Modify: `FfxivTodo/Services/ProgressStore.cs`
- Test: `FfxivTodo.Tests/Services/ProgressStoreTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using FfxivTodo.Models;
using FfxivTodo.Services;

namespace FfxivTodo.Tests.Services;

public sealed class ProgressStoreTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void SetStatus_LastQuestInChain_AutoCompletesParent()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Eden's Gate", UnlockQuestIds = [10] },
                new() { Id = 10, Name = "Quest A", QuestId = 100 },
            };

            store.SetStatus(10, ItemStatus.Completed, false, contentItems);

            var parentEntry = store.GetOrCreate(1);
            Assert.Equal(ItemStatus.Completed, parentEntry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetStatus_MidChain_DoesNotAutoCompleteParent()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Eden's Gate", UnlockQuestIds = [10, 20] },
                new() { Id = 10, Name = "Quest A", QuestId = 100 },
                new() { Id = 20, Name = "Quest B", QuestId = 200 },
            };

            store.SetStatus(10, ItemStatus.Completed, false, contentItems);

            var parentEntry = store.GetOrCreate(1);
            Assert.NotEqual(ItemStatus.Completed, parentEntry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetStatus_NotInChain_NoAutoCompletion()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Some Item" },
            };

            store.SetStatus(1, ItemStatus.Completed, false, contentItems);

            var entry = store.GetOrCreate(1);
            Assert.Equal(ItemStatus.Completed, entry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test FfxivTodo.Tests --filter "ProgressStoreTests" --no-restore -v n`
Expected: FAIL — `SetStatus` doesn't accept `IReadOnlyList<ContentItem>`

- [ ] **Step 3: Extend `SetStatus` with chain auto-completion**

Update `FfxivTodo/Services/ProgressStore.cs`:

Add a new overload that accepts content items for chain resolution:

```csharp
public void SetStatus(uint itemId, ItemStatus status, bool isManual, IReadOnlyList<ContentItem> allItems)
{
    var entry = GetOrCreate(itemId);
    entry.Status = status;
    entry.IsManual = isManual;

    if (status == ItemStatus.Completed)
    {
        var questItem = allItems.FirstOrDefault(i => i.Id == itemId);
        if (questItem?.QuestId != null)
        {
            foreach (var parent in allItems.Where(i => i.UnlockQuestIds.Contains(questItem.QuestId.Value)))
            {
                var allChainQuestsCompleted = parent.UnlockQuestIds.All(qid =>
                {
                    var chainQuest = allItems.FirstOrDefault(i => i.QuestId == qid);
                    if (chainQuest == null) return false;
                    var chainEntry = GetOrCreate(chainQuest.Id);
                    return chainEntry.Status == ItemStatus.Completed;
                });

                if (allChainQuestsCompleted)
                {
                    var parentEntry = GetOrCreate(parent.Id);
                    if (!parentEntry.IsManual)
                        parentEntry.Status = ItemStatus.Completed;
                }
            }
        }
    }
}
```

The existing `SetStatus(uint, ItemStatus, bool)` method remains unchanged for backward compatibility (used where content items aren't available, like the detail panel).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test FfxivTodo.Tests --filter "ProgressStoreTests" --no-restore -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add FfxivTodo/Services/ProgressStore.cs FfxivTodo.Tests/Services/ProgressStoreTests.cs
git commit -m "feat: extend ProgressStore.SetStatus with chain auto-completion"
```

---

### Task 12: UI — Quest Chain Group Rendering

**Files:**
- Modify: `FfxivTodo/Windows/MainWindow.cs`

This task adds:
- A next-quest indicator under content items with incomplete quest chains
- An expand toggle to show the full quest chain group
- Indented quest rows with locked/completed states

- [ ] **Step 1: Add expand state tracking**

Add a field to `MainWindow`:

```csharp
private readonly HashSet<uint> _expandedChains = [];
```

- [ ] **Step 2: Modify `DrawTreeItem` to show next-quest indicator and chain group**

After the existing `DrawTreeItem` logic (after the context popup, before `ImGui.PopID`), add chain rendering:

```csharp
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
```

- [ ] **Step 3: Update `DrawContextMenu` for quest chain items**

In `DrawContextMenu`, when marking a quest complete that's part of a chain, use the new `SetStatus` overload:

```csharp
if (entry.Status != ItemStatus.Completed &&
    ImGui.MenuItem("Mark as Complete"))
{
    _progressStore.SetStatus(item.Id, ItemStatus.Completed, true, _contentManager.Items);
    _progressStore.Save();
}
```

Similarly for "Reset to Not Started":

```csharp
if (entry.Status != ItemStatus.NotStarted &&
    ImGui.MenuItem("Reset to Not Started"))
{
    _progressStore.SetStatus(item.Id, ItemStatus.NotStarted, true, _contentManager.Items);
    _progressStore.Save();
}
```

And in `DrawDetailPanel`:

```csharp
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
```

- [ ] **Step 4: Update `DrawDetailPanel` to show quest chain info**

After the existing locked prerequisites section, add:

```csharp
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
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build FfxivTodo --no-restore -v q`
Expected: Build succeeds

- [ ] **Step 6: Commit**

```bash
git add FfxivTodo/Windows/MainWindow.cs
git commit -m "feat: add quest chain group rendering with next-quest indicator"
```

---

### Task 13: UI — Filtering Quest Chain Items

**Files:**
- Modify: `FfxivTodo/Windows/MainWindow.cs`

This task ensures:
- Quest chain members inherit parent content's category for filtering
- Search matches against both content name and quest chain names

- [ ] **Step 1: Update `FilterItems` to include chain quest names in search matching**

In the search filter check within `FilterItems`:

```csharp
if (!string.IsNullOrEmpty(_searchText))
{
    var matchesName = item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    var matchesQuest = item.UnlockQuestIds.Any(qid =>
    {
        var quest = _contentManager.Items.FirstOrDefault(i => i.QuestId == qid);
        return quest?.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true;
    });

    if (!matchesName && !matchesQuest)
        continue;
}
```

- [ ] **Step 2: Verify filtering works**

Run: `dotnet build FfxivTodo --no-restore -v q`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add FfxivTodo/Windows/MainWindow.cs
git commit -m "feat: extend search filtering to match quest chain names"
```

---

### Task 14: Integration Test — Run Full Pipeline

**Files:** None (validation only)

- [ ] **Step 1: Run the full DataBuilder pipeline**

Run: `dotnet run --project DataBuilder -- --from scratch`
Expected: Pipeline completes without errors

- [ ] **Step 2: Verify `content.json` contains `unlockQuestIds`**

Run: `grep -c unlockQuestIds FfxivTodo/Data/content.json`
Expected: At least 1 match (for overridden items)

- [ ] **Step 3: Run all tests**

Run: `dotnet test --no-restore -v n`
Expected: All PASS

- [ ] **Step 4: Commit regenerated content data if changed**

```bash
git add FfxivTodo/Data/content.json
git commit -m "chore: regenerate content.json with unlockQuestIds"
```
