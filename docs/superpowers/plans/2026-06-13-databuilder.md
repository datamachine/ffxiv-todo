# DataBuilder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a C# console tool that scrapes Consolegameswiki + XIVAPI to generate `content.json` for the FfxivTodo Dalamud plugin.

**Architecture:** 4-stage pipeline (Category Scrape → Detail Scrape → ID Resolve → Format). Intermediate JSON files at each stage allow resumability. HtmlAgilityPack for wiki parsing, System.Text.Json for serialization, xUnit for testing.

**Tech Stack:** .NET 10.0, HtmlAgilityPack, System.Text.Json, Microsoft.Extensions.Http, xUnit

---

## File Plan

```
DataBuilder/
├── DataBuilder.csproj              # Console app, net10.0
├── Program.cs                      # CLI orchestration
├── Models/
│   └── PipelineModels.cs           # All intermediate DTOs
├── Scrapers/
│   ├── WikiCategoryScraper.cs      # Stage 1: category pages
│   ├── WikiDetailScraper.cs        # Stage 2: individual quest pages
│   └── XivApiResolver.cs           # Stage 3: EDB link + XIVAPI lookups
├── Formatters/
│   └── ContentJsonFormatter.cs     # Stage 4: merge, assign IDs, output
├── Infra/
│   └── HttpClientFactory.cs        # Shared resilient HttpClient
└── Data/
    └── achievement_overrides.json  # Manual achievement ID mappings

DataBuilder.Tests/
├── DataBuilder.Tests.csproj        # xUnit test project
├── TestData/                       # HTML fixture files for wiki tests
│   ├── primal_awakening.html
│   ├── peace_for_thanalan.html
│   ├── raids_page.html
│   └── job_quests_page.html
├── Scrapers/
│   ├── WikiCategoryScraperTests.cs
│   ├── WikiDetailScraperTests.cs
│   └── XivApiResolverTests.cs
└── Formatters/
    └── ContentJsonFormatterTests.cs
```

---

### Task 1: Scaffold solution and project structure

**Files:**
- Create: `ffxiv-todo.sln`
- Create: `DataBuilder/DataBuilder.csproj`
- Create: `DataBuilder.Tests/DataBuilder.Tests.csproj`
- Create: `DataBuilder/Program.cs`
- Modify: `FfxivTodo/FfxivTodo.csproj` (add to solution)

- [ ] **Step 1: Create solution file**

```bash
cd /home/vcastellano/Projects/ffxiv-todo
dotnet new sln -n ffxiv-todo --force
```

- [ ] **Step 2: Create DataBuilder console project**

```bash
dotnet new console -n DataBuilder -o DataBuilder --framework net10.0
dotnet sln add DataBuilder/DataBuilder.csproj
```

- [ ] **Step 3: Add NuGet packages to DataBuilder**

```bash
dotnet add DataBuilder/DataBuilder.csproj package HtmlAgilityPack
dotnet add DataBuilder/DataBuilder.csproj package Microsoft.Extensions.Http
```

- [ ] **Step 4: Create DataBuilder.Tests xUnit project**

```bash
dotnet new xunit -n DataBuilder.Tests -o DataBuilder.Tests --framework net10.0
dotnet sln add DataBuilder.Tests/DataBuilder.Tests.csproj
dotnet add DataBuilder.Tests/DataBuilder.Tests.csproj reference DataBuilder/DataBuilder.csproj
```

- [ ] **Step 5: Add existing FfxivTodo plugin to solution**

```bash
dotnet sln add FfxivTodo/FfxivTodo.csproj
```

- [ ] **Step 6: Verify build**

```bash
dotnet build
```
Expected: Build succeeds (DataBuilder + DataBuilder.Tests; FfxivTodo may warn about Dalamud SDK — that's fine).

- [ ] **Step 7: Commit**

```bash
git add ffxiv-todo.sln DataBuilder/ DataBuilder.Tests/
git commit -m "scaffold: add DataBuilder and DataBuilder.Tests projects"
```

---

### Task 2: Define pipeline models

**Files:**
- Create: `DataBuilder/Models/PipelineModels.cs`

- [ ] **Step 1: Write the PipelineModels file**

```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataBuilder.Models;

public sealed class CategoryItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = string.Empty;
}

public sealed class CategoryItemsFile
{
    [JsonPropertyName("items")]
    public List<CategoryItem> Items { get; set; } = new();
}

public sealed class DetailItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public uint? Level { get; set; }

    [JsonPropertyName("locationTerritoryName")]
    public string? LocationTerritoryName { get; set; }

    [JsonPropertyName("locationMapX")]
    public float? LocationMapX { get; set; }

    [JsonPropertyName("locationMapY")]
    public float? LocationMapY { get; set; }

    [JsonPropertyName("prerequisiteNames")]
    public List<string> PrerequisiteNames { get; set; } = new();

    [JsonPropertyName("edbUrl")]
    public string? EdbUrl { get; set; }

    [JsonPropertyName("wikiUrl")]
    public string? WikiUrl { get; set; }

    [JsonPropertyName("questId")]
    public uint? QuestId { get; set; }

    [JsonPropertyName("achievementId")]
    public uint? AchievementId { get; set; }

    [JsonPropertyName("locationTerritoryId")]
    public uint? LocationTerritoryId { get; set; }
}

public sealed class DetailItemsFile
{
    [JsonPropertyName("items")]
    public List<DetailItem> Items { get; set; } = new();
}

public sealed class FormattedItem
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public uint Level { get; set; }

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("prerequisiteIds")]
    public List<uint> PrerequisiteIds { get; set; } = new();

    [JsonPropertyName("locationTerritoryId")]
    public uint? LocationTerritoryId { get; set; }

    [JsonPropertyName("locationMapX")]
    public float? LocationMapX { get; set; }

    [JsonPropertyName("locationMapY")]
    public float? LocationMapY { get; set; }

    [JsonPropertyName("questId")]
    public uint? QuestId { get; set; }

    [JsonPropertyName("achievementId")]
    public uint? AchievementId { get; set; }

    [JsonPropertyName("wikiUrl")]
    public string? WikiUrl { get; set; }
}

public sealed class FormattedItemsFile
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("items")]
    public List<FormattedItem> Items { get; set; } = new();
}

public sealed class AchievementOverride
{
    [JsonPropertyName("contentName")]
    public string ContentName { get; set; } = string.Empty;

    [JsonPropertyName("achievementId")]
    public uint AchievementId { get; set; }
}

public sealed class AchievementOverridesFile
{
    [JsonPropertyName("overrides")]
    public List<AchievementOverride> Overrides { get; set; } = new();
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build DataBuilder/DataBuilder.csproj
```
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add DataBuilder/Models/PipelineModels.cs
git commit -m "feat: define pipeline model DTOs"
```

---

### Task 3: WikiCategoryScraper — expansion detection and page fetching

**Files:**
- Create: `DataBuilder/Scrapers/WikiCategoryScraper.cs`
- Create: `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DataBuilder.Scrapers;
using Xunit;

namespace DataBuilder.Tests.Scrapers;

public class WikiCategoryScraperTests
{
    [Fact]
    public void ParseExpansionFromHeading_ReturnsCorrectExpansion()
    {
        Assert.Equal("ARR", WikiCategoryScraper.ParseExpansionFromHeading("A Realm Reborn"));
        Assert.Equal("HW", WikiCategoryScraper.ParseExpansionFromHeading("Heavensward"));
        Assert.Equal("SB", WikiCategoryScraper.ParseExpansionFromHeading("Stormblood"));
        Assert.Equal("ShB", WikiCategoryScraper.ParseExpansionFromHeading("Shadowbringers"));
        Assert.Equal("EW", WikiCategoryScraper.ParseExpansionFromHeading("Endwalker"));
        Assert.Equal("DT", WikiCategoryScraper.ParseExpansionFromHeading("Dawntrail"));
    }

    [Fact]
    public void ParseExpansionFromHeading_UnknownHeading_ReturnsNull()
    {
        Assert.Null(WikiCategoryScraper.ParseExpansionFromHeading("Some Unknown Section"));
    }

    [Fact]
    public void ParseExpansionFromHeading_CaseInsensitive()
    {
        Assert.Equal("ARR", WikiCategoryScraper.ParseExpansionFromHeading("a realm reborn"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ParseExpansionFromHeading
```
Expected: FAIL — method not found.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System;
using System.Collections.Generic;
using DataBuilder.Models;
using HtmlAgilityPack;

namespace DataBuilder.Scrapers;

public sealed class WikiCategoryScraper
{
    private readonly HttpClient _http;

    private static readonly Dictionary<string, string> KnownCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Job_Quests"] = "JobQuest",
        ["Raids"] = null!, // Handled specially — contains multiple subcategories
        ["Allied_Society_Quests"] = "BeastTribe",
        ["Side_Quests"] = "SideQuest",
        ["Feature_Quests"] = null!, // Handled specially — contains BlueUnlock+
        ["Custom_Deliveries"] = "CustomDelivery",
    };

    private static readonly Dictionary<string, string> ExpansionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a realm reborn"] = "ARR",
        ["heavensward"] = "HW",
        ["stormblood"] = "SB",
        ["shadowbringers"] = "ShB",
        ["endwalker"] = "EW",
        ["dawntrail"] = "DT",
    };

    public WikiCategoryScraper(HttpClient http)
    {
        _http = http;
    }

    public static string? ParseExpansionFromHeading(string heading)
    {
        return ExpansionMap.TryGetValue(heading, out var value) ? value : null;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ParseExpansionFromHeading
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs
git commit -m "feat: add WikiCategoryScraper with expansion detection"
```

---

### Task 4: WikiCategoryScraper — Job Quest table parsing

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs`
- Modify: `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`
- Create: `DataBuilder.Tests/TestData/job_quests_paladin.html`

- [ ] **Step 1: Save test fixture**

Save a minimal HTML snippet from the Job_Quests wiki page to `DataBuilder.Tests/TestData/job_quests_paladin.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Job Quests - FFXIV Wiki</title></head>
<body>
<div id="mw-content-text">
<h3><span id="Paladin_Quests">Paladin Quests</span></h3>
<table class="questlist">
<tr>
  <th>Quest</th>
  <th>Type</th>
  <th>Level</th>
  <th>Quest Giver</th>
  <th>Unlocks</th>
</tr>
<tr>
  <td><a href="/wiki/Paladin%27s_Pledge" title="Paladin's Pledge">Paladin's Pledge</a></td>
  <td></td>
  <td>30</td>
  <td>Lulutsu</td>
  <td>Paladin, Spirits Within</td>
</tr>
<tr>
  <td><a href="/wiki/Honor_Lost" title="Honor Lost">Honor Lost</a></td>
  <td></td>
  <td>35</td>
  <td>Jenlyns</td>
  <td>Sheltron, Oath Mastery</td>
</tr>
<tr>
  <td><a href="/wiki/Power_Struggles" title="Power Struggles">Power Struggles</a></td>
  <td></td>
  <td>40</td>
  <td>Jenlyns</td>
  <td>Prominence</td>
</tr>
</table>
</div>
</body>
</html>
```

- [ ] **Step 2: Write test for parsing job quest table**

Add to `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`:

```csharp
[Fact]
public void ParseJobQuestTable_ExtractsPaladinQuests_WithExpansionHW()
{
    var doc = new HtmlDocument();
    doc.Load("TestData/job_quests_paladin.html");
    var scraper = new WikiCategoryScraper(null!);

    var items = scraper.ParseJobQuestTable(doc.DocumentNode, "Heavensward");

    Assert.Equal(3, items.Count);
    Assert.Equal("Paladin's Pledge", items[0].Name);
    Assert.Equal("JobQuest", items[0].Category);
    Assert.Equal("HW", items[0].Expansion);
    Assert.Equal("Honor Lost", items[1].Name);
    Assert.Equal("Power Struggles", items[2].Name);
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ParseJobQuestTable
```
Expected: FAIL — method not found.

- [ ] **Step 4: Implement ParseJobQuestTable**

Add to `WikiCategoryScraper.cs`:

```csharp
using System.Web;

public List<CategoryItem> ParseJobQuestTable(HtmlNode contentNode, string expansion)
{
    var items = new List<CategoryItem>();
    var expShort = ParseExpansionFromHeading(expansion) ?? expansion;

    var h3Tags = contentNode.SelectNodes(".//h3");
    if (h3Tags == null) return items;

    foreach (var h3 in h3Tags)
    {
        var span = h3.SelectSingleNode(".//span[@id]");
        if (span == null) continue;

        var current = h3;
        HtmlNode? table = null;
        while ((current = current.NextSibling) != null)
        {
            if (current.Name == "table" && current.GetAttributeValue("class", "").Contains("questlist"))
            {
                table = current;
                break;
            }
            if (current.Name == "h3" || current.Name == "h2")
                break;
        }

        if (table == null) continue;

        var rows = table.SelectNodes(".//tr");
        if (rows == null) continue;

        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 1) continue;

            var link = cells[0].SelectSingleNode(".//a");
            if (link == null) continue;

            var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
            items.Add(new CategoryItem
            {
                Name = name,
                Category = "JobQuest",
                Expansion = expShort
            });
        }
    }

    return items;
}
```

- [ ] **Step 5: Run test to verify it passes**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ParseJobQuestTable
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs DataBuilder.Tests/
git commit -m "feat: add Job Quest table parsing to WikiCategoryScraper"
```

---

### Task 5: WikiCategoryScraper — Raids table parsing

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs`
- Modify: `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`
- Create: `DataBuilder.Tests/TestData/raids_page.html`

- [ ] **Step 1: Save test fixture**

Save a minimal HTML snippet from the Raids wiki page to `DataBuilder.Tests/TestData/raids_page.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Raids - FFXIV Wiki</title></head>
<body>
<div id="mw-content-text">
<h2><span id="Normal_Raids">Normal Raids</span></h2>
<h3><span id="A_Realm_Reborn">A Realm Reborn</span></h3>
<table>
<tr>
  <th>Duty Name</th>
  <th>Level</th>
  <th>Unlock</th>
</tr>
<tr>
  <td>The Binding Coil of Bahamut</td>
  <td>50</td>
  <td><a href="/wiki/Primal_Awakening">Primal Awakening</a></td>
</tr>
<tr>
  <td>The Second Coil of Bahamut</td>
  <td>50</td>
  <td><a href="/wiki/Another_Turn_in_the_Coil">Another Turn in the Coil</a></td>
</tr>
</table>
<h3><span id="Heavensward">Heavensward</span></h3>
<table>
<tr>
  <th>Duty Name</th>
  <th>Level</th>
  <th>Unlock</th>
</tr>
<tr>
  <td>Alexander: Gordias</td>
  <td>60</td>
  <td><a href="/wiki/Disarmed">Disarmed</a></td>
</tr>
</table>
<h2><span id="Alliance_Raids">Alliance Raids</span></h2>
<h3><span id="A_Realm_Reborn_2">A Realm Reborn</span></h3>
<table>
<tr>
  <th>Duty Name</th>
  <th>Level</th>
  <th>Unlock</th>
</tr>
<tr>
  <td>The Labyrinth of the Ancients</td>
  <td>50</td>
  <td><a href="/wiki/Labyrinth_of_the_Ancients">Labyrinth of the Ancients</a></td>
</tr>
</table>
</div>
</body>
</html>
```

- [ ] **Step 2: Write test for parsing raids table**

Add to `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`:

```csharp
[Fact]
public void ParseRaidsPage_ExtractsNormalAndAllianceRaids()
{
    var doc = new HtmlDocument();
    doc.Load("TestData/raids_page.html");
    var scraper = new WikiCategoryScraper(null!);

    var items = scraper.ParseRaidsPage(doc.DocumentNode);

    Assert.Equal(3, items.Count);

    var bahamut = items.First(i => i.Name == "The Binding Coil of Bahamut");
    Assert.Equal("RaidSeries", bahamut.Category);
    Assert.Equal("ARR", bahamut.Expansion);

    var alexander = items.First(i => i.Name == "Alexander: Gordias");
    Assert.Equal("RaidSeries", alexander.Category);
    Assert.Equal("HW", alexander.Expansion);

    var labyrinth = items.First(i => i.Name == "The Labyrinth of the Ancients");
    Assert.Equal("AllianceRaid", labyrinth.Category);
    Assert.Equal("ARR", labyrinth.Expansion);
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ParseRaidsPage
```
Expected: FAIL — method not found.

- [ ] **Step 4: Implement ParseRaidsPage**

Add to `WikiCategoryScraper.cs`:

```csharp
public List<CategoryItem> ParseRaidsPage(HtmlNode contentNode)
{
    var items = new List<CategoryItem>();
    var currentSection = string.Empty; // "Normal_Raids", "Alliance_Raids", etc.
    var currentExpansion = string.Empty;

    var headings = contentNode.SelectNodes(".//h2|.//h3");
    if (headings == null) return items;

    foreach (var heading in headings)
    {
        var span = heading.SelectSingleNode(".//span[@id]");
        if (span == null) continue;
        var sectionId = span.GetAttributeValue("id", "");

        if (sectionId == "Normal_Raids") { currentSection = "RaidSeries"; continue; }
        if (sectionId == "Alliance_Raids") { currentSection = "AllianceRaid"; continue; }
        if (sectionId.StartsWith("Savage_Raids")) { currentSection = string.Empty; continue; } // Skip savage
        if (sectionId.StartsWith("Ultimate_Raids")) { currentSection = string.Empty; continue; } // Skip ultimate

        var exp = ParseExpansionFromHeading(sectionId.Replace("_", " "));
        if (exp != null)
        {
            currentExpansion = exp;
            continue;
        }

        // After a h2/h3 that sets section+expansion, find the next table
        if (string.IsNullOrEmpty(currentSection)) continue;

        var current = heading;
        HtmlNode? table = null;
        while ((current = current.NextSibling) != null)
        {
            if (current.Name == "table")
            {
                table = current;
                break;
            }
            if (current.Name == "h2" || current.Name == "h3")
                break;
        }

        if (table == null) continue;

        var rows = table.SelectNodes(".//tr");
        if (rows == null) continue;

        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 1) continue;

            var dutyName = HttpUtility.HtmlDecode(cells[0].InnerText.Trim());
            if (string.IsNullOrWhiteSpace(dutyName)) continue;

            items.Add(new CategoryItem
            {
                Name = dutyName,
                Category = currentSection,
                Expansion = currentExpansion
            });
        }
    }

    return items;
}
```

Note: The `heading` iteration approach walks all h2/h3 nodes in document order, tracking current section and expansion. This handles the nested structure of the Raids page (h2 for raid type, h3 for expansion, then table).

- [ ] **Step 5: Run test to verify it passes**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ParseRaidsPage
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs DataBuilder.Tests/
git commit -m "feat: add Raids table parsing to WikiCategoryScraper"
```

---

### Task 6: WikiCategoryScraper — Beast Tribe / Custom Delivery / Feature Quest link extraction

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs`
- Modify: `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`
- Create: `DataBuilder.Tests/TestData/allied_society_page.html`
- Create: `DataBuilder.Tests/TestData/feature_quests_dungeons.html`

- [ ] **Step 1: Save test fixture for Allied Society page**

Save to `DataBuilder.Tests/TestData/allied_society_page.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Allied Society Quests - FFXIV Wiki</title></head>
<body>
<div id="mw-content-text">
<h2><span id="A_Realm_Reborn_Allied_Societies">A Realm Reborn Allied Societies</span></h2>
<h3><span id="Amalj.27aa_Daily_Quests">Amalj'aa Daily Quests</span></h3>
<p>To unlock the Amalj'aa quests players must complete the level 43 quest
<a href="/wiki/Peace_for_Thanalan" title="Peace for Thanalan">Peace for Thanalan</a>,
which begins with talking to Swift in Ul'dah - Steps of Nald (X:8.4, Y:8.9).</p>
<h3><span id="Sylph_Daily_Quests">Sylph Daily Quests</span></h3>
<p>To unlock the Sylph quests players must complete the level 42
<a href="/wiki/Seeking_Solace" title="Seeking Solace">Seeking Solace</a>,
which begins with talking to Vorsaile Heuloix in New Gridania (X:9.7, Y:11.1).</p>
<h2><span id="Heavensward_Allied_Societies">Heavensward Allied Societies</span></h2>
<h3><span id="Vanu_Vanu_Daily_Quests">Vanu Vanu Daily Quests</span></h3>
<p>To unlock the Vanu Vanu quests players must complete the level 50 quest
<a href="/wiki/Three_Beaks_to_the_Wind" title="Three Beaks to the Wind">Three Beaks to the Wind</a>,
which begins with talking to Sonu Vanu in The Sea of Clouds (X:11.7, Y:14.8).</p>
</div>
</body>
</html>
```

- [ ] **Step 2: Save test fixture for Feature Quests page (dungeons subsection)**

Save to `DataBuilder.Tests/TestData/feature_quests_dungeons.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Feature Quests - FFXIV Wiki</title></head>
<body>
<div id="mw-content-text">
<h2><span id="Other_Instances">Other Instances</span></h2>
<h3><span id="Dungeons">Dungeons</span></h3>
<table>
<tr><th>Quest</th><th>Type</th><th>Level</th><th>Quest Giver</th><th>Unlocks</th></tr>
<tr>
  <td><a href="/wiki/Hallo_Halatali">Hallo Halatali</a></td>
  <td></td><td>20</td><td>Nedrick Ironheart</td><td>Halatali</td>
</tr>
<tr>
  <td><a href="/wiki/Braving_New_Depths">Braving New Depths</a></td>
  <td></td><td>35</td><td>Nedrick Ironheart</td><td>The Sunken Temple of Qarn</td>
</tr>
</table>
</div>
</body>
</html>
```

- [ ] **Step 3: Write tests for Beast Tribe and Feature Quest parsing**

Add to `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`:

```csharp
[Fact]
public void ParseAlliedSocietyPage_ExtractsUnlockQuests()
{
    var doc = new HtmlDocument();
    doc.Load("TestData/allied_society_page.html");
    var scraper = new WikiCategoryScraper(null!);

    var items = scraper.ParseAlliedSocietyPage(doc.DocumentNode);

    Assert.Equal(3, items.Count);
    Assert.Equal("Peace for Thanalan", items[0].Name);
    Assert.Equal("BeastTribe", items[0].Category);
    Assert.Equal("ARR", items[0].Expansion);
    Assert.Equal("Seeking Solace", items[1].Name);
    Assert.Equal("ARR", items[1].Expansion);
    Assert.Equal("Three Beaks to the Wind", items[2].Name);
    Assert.Equal("HW", items[2].Expansion);
}

[Fact]
public void ParseFeatureQuestsPage_ExtractsDungeonUnlockQuests()
{
    var doc = new HtmlDocument();
    doc.Load("TestData/feature_quests_dungeons.html");
    var scraper = new WikiCategoryScraper(null!);

    var items = scraper.ParseFeatureQuestsPage(doc.DocumentNode, "ARR");

    Assert.Equal(2, items.Count);
    Assert.Equal("Hallo Halatali", items[0].Name);
    Assert.Equal("BlueUnlock", items[0].Category);
    Assert.Equal("ARR", items[0].Expansion);
    Assert.Equal("Braving New Depths", items[1].Name);
}
```

- [ ] **Step 4: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter "ParseAlliedSocietyPage|ParseFeatureQuestsPage"
```
Expected: FAIL — methods not found.

- [ ] **Step 5: Implement ParseAlliedSocietyPage**

Add to `WikiCategoryScraper.cs`:

```csharp
public List<CategoryItem> ParseAlliedSocietyPage(HtmlNode contentNode)
{
    var items = new List<CategoryItem>();
    var currentExpansion = string.Empty;

    var headings = contentNode.SelectNodes(".//h2|.//h3");
    if (headings == null) return items;

    foreach (var heading in headings)
    {
        var span = heading.SelectSingleNode(".//span[@id]");
        if (span == null) continue;
        var sectionId = span.GetAttributeValue("id", "");

        var exp = ParseExpansionFromHeading(sectionId.Replace("_", " "));
        if (exp != null)
        {
            currentExpansion = exp;
            continue;
        }

        // Look for unlock quest links in the paragraph after this heading
        var para = heading.NextSibling;
        while (para != null && para.Name != "p" && para.Name != "h2" && para.Name != "h3")
            para = para.NextSibling;

        if (para == null || para.Name != "p") continue;

        // Find links within "To unlock ... must complete the level XX quest <link>"
        var links = para.SelectNodes(".//a[contains(@href,'/wiki/')]");
        if (links == null) continue;

        foreach (var link in links)
        {
            var questName = HttpUtility.HtmlDecode(link.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(questName)) continue;

            items.Add(new CategoryItem
            {
                Name = questName,
                Category = "BeastTribe",
                Expansion = currentExpansion
            });
            break; // Only first link is the unlock quest
        }
    }

    return items;
}
```

- [ ] **Step 6: Implement ParseFeatureQuestsPage**

Add to `WikiCategoryScraper.cs`:

```csharp
public List<CategoryItem> ParseFeatureQuestsPage(HtmlNode contentNode, string defaultExpansion)
{
    var items = new List<CategoryItem>();
    var currentExpansion = ParseExpansionFromHeading(defaultExpansion) ?? defaultExpansion;
    var currentCategory = "BlueUnlock";

    var headings = contentNode.SelectNodes(".//h2|.//h3");
    if (headings == null) return items;

    foreach (var heading in headings)
    {
        var span = heading.SelectSingleNode(".//span[@id]");
        if (span == null) continue;
        var sectionId = span.GetAttributeValue("id", "");

        var exp = ParseExpansionFromHeading(sectionId.Replace("_", " "));
        if (exp != null) { currentExpansion = exp; continue; }

        // Skip sections we don't track (Job/Role quests handled elsewhere)
        if (sectionId.Contains("Class") || sectionId.Contains("Job") || sectionId.Contains("Role"))
        {
            currentCategory = string.Empty; // Skip
            continue;
        }
        if (sectionId.Contains("Chronicles") || sectionId.Contains("Trials")
            || sectionId.Contains("Normal_Raids") || sectionId.Contains("Alliance_Raids"))
        {
            currentCategory = string.Empty; // Handled by ParseRaidsPage
            continue;
        }

        // Reset to BlueUnlock for other sections (Dungeons, PvP, Hunt, etc.)
        currentCategory = "BlueUnlock";

        // Find the next table
        var current = heading;
        HtmlNode? table = null;
        while ((current = current.NextSibling) != null)
        {
            if (current.Name == "table")
            {
                table = current;
                break;
            }
            if (current.Name == "h2" || current.Name == "h3")
                break;
        }

        if (table == null || string.IsNullOrEmpty(currentCategory)) continue;

        var rows = table.SelectNodes(".//tr");
        if (rows == null) continue;

        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 1) continue;

            var link = cells[0].SelectSingleNode(".//a");
            if (link == null) continue;

            var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
            items.Add(new CategoryItem
            {
                Name = name,
                Category = currentCategory,
                Expansion = currentExpansion
            });
        }
    }

    return items;
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter "ParseAlliedSocietyPage|ParseFeatureQuestsPage"
```
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs DataBuilder.Tests/
git commit -m "feat: add BeastTribe and FeatureQuest parsing to WikiCategoryScraper"
```

---

### Task 7: WikiCategoryScraper — HTTP fetching and full pipeline orchestration

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs`
- Modify: `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`

This task adds the `ScrapeAllAsync` method that fetches all category pages from the wiki and combines results into `category_items.json`.

- [ ] **Step 1: Write test for ScrapeAllAsync (integration-style, using mock HTTP)**

Add to `DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs`:

```csharp
[Fact]
public async Task ScrapeAllAsync_CombinesAllCategories()
{
    // Use a handler that returns fixture files based on request URL
    var handler = new MockHttpHandler(req =>
    {
        var path = req.RequestUri?.AbsolutePath;
        if (path?.Contains("Job_Quests") == true)
            return File.ReadAllText("TestData/job_quests_paladin.html");
        if (path?.Contains("Raids") == true)
            return File.ReadAllText("TestData/raids_page.html");
        if (path?.Contains("Allied_Society") == true)
            return File.ReadAllText("TestData/allied_society_page.html");
        if (path?.Contains("Feature_Quests") == true)
            return File.ReadAllText("TestData/feature_quests_dungeons.html");
        return "<html><body></body></html>";
    });

    var http = new HttpClient(handler) { BaseAddress = new Uri("https://ffxiv.consolegameswiki.com") };
    var scraper = new WikiCategoryScraper(http);

    var items = await scraper.ScrapeAllAsync();

    Assert.True(items.Count > 0);
    Assert.Contains(items, i => i.Category == "JobQuest");
    Assert.Contains(items, i => i.Category == "RaidSeries");
    Assert.Contains(items, i => i.Category == "AllianceRaid");
    Assert.Contains(items, i => i.Category == "BeastTribe");
    Assert.Contains(items, i => i.Category == "BlueUnlock");
}
```

Also add a helper mock handler class:

```csharp
private class MockHttpHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, string> _handler;

    public MockHttpHandler(Func<HttpRequestMessage, string> handler)
    {
        _handler = handler;
        InnerHandler = new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = _handler(request);
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "text/html")
        };
        return Task.FromResult(response);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ScrapeAllAsync
```
Expected: FAIL — method not found.

- [ ] **Step 3: Implement ScrapeAllAsync**

Add to `WikiCategoryScraper.cs`:

```csharp
private static readonly string WikiBase = "https://ffxiv.consolegameswiki.com";

public async Task<List<CategoryItem>> ScrapeAllAsync()
{
    var allItems = new List<CategoryItem>();

    // Job Quests
    var jobItems = await FetchAndParseAsync("/wiki/Job_Quests", doc =>
    {
        var expHeads = doc.DocumentNode.SelectNodes(".//h2");
        if (expHeads == null) return new List<CategoryItem>();
        var result = new List<CategoryItem>();
        foreach (var h2 in expHeads)
        {
            var span = h2.SelectSingleNode(".//span[@id]");
            if (span == null) continue;
            var exp = ParseExpansionFromHeading(span.GetAttributeValue("id", "").Replace("_", " "));
            if (exp != null)
                result.AddRange(ParseJobQuestTable(doc.DocumentNode, exp));
        }
        // If no expansion sections found, parse with default
        if (result.Count == 0)
            result.AddRange(ParseJobQuestTable(doc.DocumentNode, "ARR"));
        return result;
    });
    allItems.AddRange(jobItems);

    // Raids (Normal + Alliance)
    var raidItems = await FetchAndParseAsync("/wiki/Raids", doc =>
        ParseRaidsPage(doc.DocumentNode));
    allItems.AddRange(raidItems);

    // Allied Society / Beast Tribes
    var beastItems = await FetchAndParseAsync("/wiki/Allied_Society_Quests", doc =>
        ParseAlliedSocietyPage(doc.DocumentNode));
    allItems.AddRange(beastItems);

    // Feature Quests (BlueUnlock — dungeons, GC, hunt, etc.)
    var blueItems = await FetchAndParseAsync("/wiki/Feature_Quests", doc =>
    {
        var items = new List<CategoryItem>();
        foreach (var exp in new[] { "ARR", "HW", "SB", "ShB", "EW", "DT" })
            items.AddRange(ParseFeatureQuestsPage(doc.DocumentNode, exp));
        return items;
    });
    allItems.AddRange(blueItems);

    // Custom Deliveries
    var deliveryItems = await FetchAndParseAsync("/wiki/Custom_Deliveries", doc =>
    {
        var items = new List<CategoryItem>();
        foreach (var exp in new[] { "HW", "SB", "ShB", "EW", "DT" })
            items.AddRange(ParseFeatureQuestsPage(doc.DocumentNode, exp));
        return items;
    });
    allItems.AddRange(deliveryItems);

    // Side Quests
    var sideItems = await FetchAndParseAsync("/wiki/Side_Quests", doc =>
        ParseFeatureQuestsPage(doc.DocumentNode, "ARR"));
    allItems.AddRange(sideItems);

    // Deduplicate by name
    return allItems.DistinctBy(i => i.Name).ToList();
}

private async Task<List<CategoryItem>> FetchAndParseAsync(
    string path, Func<HtmlDocument, List<CategoryItem>> parser)
{
    var html = await _http.GetStringAsync($"{WikiBase}{path}");
    var doc = new HtmlDocument();
    doc.LoadHtml(html);
    return parser(doc);
}
```

Note: The `ScrapeAllAsync` method uses the `DistinctBy` LINQ method (available in .NET 6+). The Feature Quests page is parsed per-expansion to ensure expansion-scoped sections are properly identified. The `ParseFeatureQuestsPage` method is reused for Custom Deliveries and Side Quests since they share the same table structure.

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ScrapeAllAsync
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs DataBuilder.Tests/Scrapers/WikiCategoryScraperTests.cs
git commit -m "feat: add ScrapeAllAsync to WikiCategoryScraper"
```

---

### Task 8: WikiDetailScraper — infobox parsing from individual quest pages

**Files:**
- Create: `DataBuilder/Scrapers/WikiDetailScraper.cs`
- Create: `DataBuilder.Tests/Scrapers/WikiDetailScraperTests.cs`
- Create: `DataBuilder.Tests/TestData/primal_awakening.html`

- [ ] **Step 1: Save test fixture**

Save to `DataBuilder.Tests/TestData/primal_awakening.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Primal Awakening - FFXIV Wiki</title></head>
<body>
<div id="mw-content-text">
<table class="infobox">
<tr><th>Quest giver</th><td>Urianger</td></tr>
<tr><th>Location</th><td>The Waking Sands&nbsp;(X:6.0, Y:4.9)</td></tr>
<tr><th>Quest line</th><td><a href="/wiki/Bahamut_Quests">Bahamut Quests</a></td></tr>
<tr><th>Level</th><td>50</td></tr>
<tr><th>Requirements</th><td><a href="/wiki/The_Navel_(Hard)">The Navel (Hard)</a> cleared</td></tr>
<tr><th>Previous quest</th><td><a href="/wiki/In_a_Titan_Spot">In a Titan Spot</a></td></tr>
<tr><th>Next quest</th><td><a href="/wiki/Alisaie%27s_Pledge">Alisaie's Pledge</a></td></tr>
<tr><th>Patch</th><td>2.0</td></tr>
<tr><th>Links</th><td><a href="https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/" class="external text">EDB</a> <a href="https://www.garlandtools.org/db/#quest/65586" class="external text">GT</a></td></tr>
</table>
<h2><span id="Rewards">Rewards</span></h2>
<p>Unlocks:</p>
<ul>
<li><a href="/wiki/The_Binding_Coil_of_Bahamut_-_Turn_1">The Binding Coil of Bahamut - Turn 1</a></li>
<li><a href="/wiki/The_Binding_Coil_of_Bahamut_-_Turn_2">The Binding Coil of Bahamut - Turn 2</a></li>
</ul>
<div id="mw-normal-catlinks">
<ul>
<li><a href="/wiki/Category:Feature_quests">Feature quests</a></li>
<li><a href="/wiki/Category:Bahamut_Quests">Bahamut Quests</a></li>
</ul>
</div>
</div>
</body>
</html>
```

- [ ] **Step 2: Write tests for infobox parsing**

Create `DataBuilder.Tests/Scrapers/WikiDetailScraperTests.cs`:

```csharp
using DataBuilder.Scrapers;
using HtmlAgilityPack;
using Xunit;

namespace DataBuilder.Tests.Scrapers;

public class WikiDetailScraperTests
{
    [Fact]
    public void ParseInfobox_ExtractsLevel()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal((uint)50, result.Level);
    }

    [Fact]
    public void ParseInfobox_ExtractsLocationCoords()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal("The Waking Sands", result.LocationTerritoryName);
        Assert.Equal(6.0f, result.LocationMapX);
        Assert.Equal(4.9f, result.LocationMapY);
    }

    [Fact]
    public void ParseInfobox_ExtractsPrerequisites()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Single(result.PrerequisiteNames);
        Assert.Equal("The Navel (Hard)", result.PrerequisiteNames[0]);
    }

    [Fact]
    public void ParseInfobox_ExtractsEdbUrl()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal("https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/", result.EdbUrl);
    }

    [Fact]
    public void ParseInfobox_SetsWikiUrl()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal("https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening", result.WikiUrl);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter WikiDetailScraper
```
Expected: FAIL — class not found.

- [ ] **Step 4: Implement WikiDetailScraper**

Create `DataBuilder/Scrapers/WikiDetailScraper.cs`:

```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DataBuilder.Models;
using HtmlAgilityPack;

namespace DataBuilder.Scrapers;

public sealed class WikiDetailScraper
{
    private static readonly Regex CoordRegex = new(
        @"\((?:X|x):\s*([\d.]+),\s*(?:Y|y):\s*([\d.]+)\)",
        RegexOptions.Compiled);

    public DetailItem ParseDetailPage(HtmlNode contentNode, string wikiUrl)
    {
        var item = new DetailItem { WikiUrl = wikiUrl };

        var infobox = contentNode.SelectSingleNode(".//table[contains(@class,'infobox')]")
                     ?? contentNode.SelectSingleNode(".//table");

        if (infobox != null)
        {
            foreach (var row in infobox.SelectNodes(".//tr") ?? Enumerable.Empty<HtmlNode>())
            {
                var th = row.SelectSingleNode(".//th");
                var td = row.SelectSingleNode(".//td");
                if (th == null || td == null) continue;

                var label = th.InnerText.Trim().ToLowerInvariant();
                var value = td.InnerText.Trim();

                switch (label)
                {
                    case "level":
                        if (uint.TryParse(value, out var level))
                            item.Level = level;
                        break;
                    case "location":
                        ParseLocation(value, item);
                        break;
                    case "requirements":
                        ParsePrerequisites(td, item);
                        break;
                    case "links":
                        ParseLinks(td, item);
                        break;
                }
            }
        }

        return item;
    }

    private void ParseLocation(string raw, DetailItem item)
    {
        var match = CoordRegex.Match(raw);
        if (match.Success)
        {
            var ampIdx = raw.IndexOf('&');
            var territoryName = ampIdx > 0
                ? raw[..ampIdx].Trim()
                : raw[..raw.IndexOf('(')].Trim();

            item.LocationTerritoryName = territoryName;
            item.LocationMapX = float.Parse(match.Groups[1].Value);
            item.LocationMapY = float.Parse(match.Groups[2].Value);
        }
        else
        {
            item.LocationTerritoryName = raw;
        }
    }

    private void ParsePrerequisites(HtmlNode td, DetailItem item)
    {
        var links = td.SelectNodes(".//a");
        if (links != null)
        {
            foreach (var link in links)
            {
                var name = System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(name))
                    item.PrerequisiteNames.Add(name);
            }
        }

        // If no links, try parsing text (e.g., "Some quest cleared")
        if (item.PrerequisiteNames.Count == 0)
        {
            var text = td.InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var cleaned = text.Replace(" cleared", "").Replace(" completed", "").Trim();
                item.PrerequisiteNames.Add(cleaned);
            }
        }
    }

    private void ParseLinks(HtmlNode td, DetailItem item)
    {
        var links = td.SelectNodes(".//a[contains(@class,'external')]");
        if (links == null) return;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "");
            if (href.Contains("lodestone") && href.Contains("/quest/"))
                item.EdbUrl = href;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter WikiDetailScraper
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DataBuilder/Scrapers/WikiDetailScraper.cs DataBuilder.Tests/Scrapers/WikiDetailScraperTests.cs DataBuilder.Tests/TestData/primal_awakening.html
git commit -m "feat: add WikiDetailScraper with infobox parsing"
```

---

### Task 9: WikiDetailScraper — batch scraping with rate limiting

**Files:**
- Modify: `DataBuilder/Scrapers/WikiDetailScraper.cs`
- Modify: `DataBuilder.Tests/Scrapers/WikiDetailScraperTests.cs`

- [ ] **Step 1: Write test for ScrapeDetailsAsync**

Add to `DataBuilder.Tests/Scrapers/WikiDetailScraperTests.cs`:

```csharp
[Fact]
public async Task ScrapeDetailsAsync_EnrichesCategoryItems()
{
    var handler = new MockHttpHandler(req =>
        File.ReadAllText("TestData/primal_awakening.html"));

    var http = new HttpClient(handler)
        { BaseAddress = new Uri("https://ffxiv.consolegameswiki.com") };
    var scraper = new WikiDetailScraper(http);

    var categoryItems = new List<CategoryItem>
    {
        new() { Name = "Primal Awakening", Category = "RaidSeries", Expansion = "ARR" }
    };

    var results = await scraper.ScrapeDetailsAsync(categoryItems);

    Assert.Single(results);
    Assert.Equal((uint)50, results[0].Level);
    Assert.Equal("ARR", results[0].Expansion);
    Assert.Equal("RaidSeries", results[0].Category);
    Assert.NotNull(results[0].WikiUrl);
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ScrapeDetailsAsync
```
Expected: FAIL — method not found.

- [ ] **Step 3: Implement ScrapeDetailsAsync**

Add to `WikiDetailScraper.cs`:

```csharp
private readonly HttpClient? _http;
private const string WikiBase = "https://ffxiv.consolegameswiki.com";

public WikiDetailScraper() { }

public WikiDetailScraper(HttpClient http)
{
    _http = http;
}

public async Task<List<DetailItem>> ScrapeDetailsAsync(List<CategoryItem> categoryItems)
{
    if (_http == null) throw new InvalidOperationException("HttpClient not configured");

    var results = new List<DetailItem>();

    foreach (var catItem in categoryItems)
    {
        var slug = catItem.Name.Replace(' ', '_').Replace("'", "%27");
        var url = $"{WikiBase}/wiki/{slug}";

        try
        {
            var html = await _http.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var detail = ParseDetailPage(doc.DocumentNode, url);
            detail.Name = catItem.Name;
            detail.Category = catItem.Category;
            detail.Expansion = catItem.Expansion;

            results.Add(detail);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"WARN: Failed to fetch {url}: {ex.Message}");
            results.Add(new DetailItem
            {
                Name = catItem.Name,
                Category = catItem.Category,
                Expansion = catItem.Expansion,
                WikiUrl = url
            });
        }

        await Task.Delay(1000); // Rate limit: 1 req/sec
    }

    return results;
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ScrapeDetailsAsync
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DataBuilder/Scrapers/WikiDetailScraper.cs DataBuilder.Tests/Scrapers/WikiDetailScraperTests.cs
git commit -m "feat: add batch scraping with rate limiting to WikiDetailScraper"
```

---

### Task 10: XivApiResolver — EDB link extraction and XIVAPI lookups

**Files:**
- Create: `DataBuilder/Scrapers/XivApiResolver.cs`
- Create: `DataBuilder.Tests/Scrapers/XivApiResolverTests.cs`
- Create: `DataBuilder/Data/achievement_overrides.json`

- [ ] **Step 1: Write tests**

Create `DataBuilder.Tests/Scrapers/XivApiResolverTests.cs`:

```csharp
using DataBuilder.Scrapers;
using Xunit;

namespace DataBuilder.Tests.Scrapers;

public class XivApiResolverTests
{
    [Fact]
    public void ExtractQuestIdFromEdbUrl_ValidUrl_ReturnsId()
    {
        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/";
        var id = XivApiResolver.ExtractQuestIdFromEdbUrl(url);
        Assert.Equal((uint)65586, id);
    }

    [Fact]
    public void ExtractQuestIdFromEdbUrl_InvalidUrl_ReturnsNull()
    {
        var url = "https://example.com/something";
        var id = XivApiResolver.ExtractQuestIdFromEdbUrl(url);
        Assert.Null(id);
    }

    [Fact]
    public void ExtractQuestIdFromEdbUrl_NullUrl_ReturnsNull()
    {
        var id = XivApiResolver.ExtractQuestIdFromEdbUrl(null);
        Assert.Null(id);
    }

    [Fact]
    public async Task ResolveAsync_ExtractsQuestIdFromEdbLink()
    {
        var handler = new MockHttpHandler(req =>
        {
            return """{"Results": [{"ID": 690, "Name": "Mapping the Realm", "Url": "/achievement/690"}]}""";
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://xivapi.com") };
        var resolver = new XivApiResolver(http, "test_key");

        var item = new Models.DetailItem
        {
            Name = "Test Duty",
            EdbUrl = "https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/"
        };

        await resolver.ResolveAsync(item);

        Assert.Equal((uint)65586, item.QuestId);
    }
}
```

- [ ] **Step 2: Create achievement_overrides.json**

Create `DataBuilder/Data/achievement_overrides.json`:

```json
{
  "overrides": []
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter XivApiResolver
```
Expected: FAIL — class not found.

- [ ] **Step 4: Implement XivApiResolver**

Create `DataBuilder/Scrapers/XivApiResolver.cs`:

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class XivApiResolver
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly Dictionary<string, uint> _territoryCache = new();
    private readonly Dictionary<string, uint> _achievementCache = new();

    private static readonly Regex EdbQuestIdRegex = new(
        @"/quest/(\d+)/",
        RegexOptions.Compiled);

    private const string XivApiBase = "https://xivapi.com";

    public XivApiResolver(HttpClient http, string? apiKey = null)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public static uint? ExtractQuestIdFromEdbUrl(string? url)
    {
        if (url == null) return null;
        var match = EdbQuestIdRegex.Match(url);
        if (match.Success && uint.TryParse(match.Groups[1].Value, out var id))
            return id;
        return null;
    }

    public async Task ResolveAsync(DetailItem item)
    {
        // Quest ID from EDB link
        if (item.QuestId == null && item.EdbUrl != null)
            item.QuestId = ExtractQuestIdFromEdbUrl(item.EdbUrl);

        // Quest ID fallback from XIVAPI
        if (item.QuestId == null)
            item.QuestId = await SearchXivApiAsync("quest", item.Name);

        // Achievement ID from XIVAPI
        if (item.AchievementId == null)
        {
            var achName = DeriveAchievementName(item.Name, item.Category);
            item.AchievementId = await SearchXivApiAsync("achievement", achName);
        }

        // Territory ID from XIVAPI
        if (item.LocationTerritoryId == null && item.LocationTerritoryName != null)
        {
            if (!_territoryCache.TryGetValue(item.LocationTerritoryName, out var terrId))
            {
                terrId = (await SearchXivApiAsync("territorytype", item.LocationTerritoryName)) ?? 0;
                if (terrId > 0) _territoryCache[item.LocationTerritoryName] = terrId;
            }
            item.LocationTerritoryId = terrId > 0 ? terrId : null;
        }
    }

    private async Task<uint?> SearchXivApiAsync(string index, string name)
    {
        try
        {
            var url = $"{XivApiBase}/search?string={Uri.EscapeDataString(name)}&indexes={index}&limit=1";
            if (_apiKey != null) url += $"&private_key={_apiKey}";

            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                if (first.TryGetProperty("ID", out var idEl))
                    return idEl.GetUInt32();
            }
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"WARN: XIVAPI search failed for {index}/{name}: {ex.Message}");
        }

        return null;
    }

    private static string DeriveAchievementName(string contentName, string category)
    {
        return category switch
        {
            "RaidSeries" or "AllianceRaid" or "TrialSeries"
                => $"Mapping the Realm: {contentName}",
            _ => contentName
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter XivApiResolver
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DataBuilder/Scrapers/XivApiResolver.cs DataBuilder.Tests/Scrapers/XivApiResolverTests.cs DataBuilder/Data/achievement_overrides.json
git commit -m "feat: add XivApiResolver with EDB extraction and XIVAPI lookups"
```

---

### Task 11: ContentJsonFormatter — merge, assign IDs, resolve prerequisites, output

**Files:**
- Create: `DataBuilder/Formatters/ContentJsonFormatter.cs`
- Create: `DataBuilder.Tests/Formatters/ContentJsonFormatterTests.cs`

- [ ] **Step 1: Write tests**

Create `DataBuilder.Tests/Formatters/ContentJsonFormatterTests.cs`:

```csharp
using DataBuilder.Formatters;
using DataBuilder.Models;
using Xunit;

namespace DataBuilder.Tests.Formatters;

public class ContentJsonFormatterTests
{
    [Fact]
    public void Format_AssignsSequentialIds()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Quest A", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "Quest B", Category = "JobQuest", Expansion = "ARR", Level = 35 },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Equal(1u, result.Items[0].Id);
        Assert.Equal(2u, result.Items[1].Id);
    }

    [Fact]
    public void Format_ResolvesPrerequisiteNamesToIds()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Quest A", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new()
            {
                Name = "Quest B", Category = "JobQuest", Expansion = "ARR", Level = 35,
                PrerequisiteNames = new List<string> { "Quest A" }
            },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Single(result.Items[1].PrerequisiteIds);
        Assert.Equal(1u, result.Items[1].PrerequisiteIds[0]);
    }

    [Fact]
    public void Format_SkipsItemsMissingRequiredFields()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Valid", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "No Category", Category = "", Expansion = "ARR", Level = 30 },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Single(result.Items);
        Assert.Equal("Valid", result.Items[0].Name);
    }

    [Fact]
    public void Format_SortsByExpansionCategoryLevelName()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Z Quest", Category = "RaidSeries", Expansion = "ARR", Level = 50 },
            new() { Name = "A Quest", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "B Quest", Category = "JobQuest", Expansion = "HW", Level = 60 },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Equal("A Quest", result.Items[0].Name);
        Assert.Equal("Z Quest", result.Items[1].Name);
        Assert.Equal("B Quest", result.Items[2].Name);
    }

    [Fact]
    public void Format_UnresolvablePrereq_OmittedWithWarning()
    {
        var items = new List<DetailItem>
        {
            new()
            {
                Name = "Quest", Category = "JobQuest", Expansion = "ARR", Level = 30,
                PrerequisiteNames = new List<string> { "Nonexistent Quest" }
            },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Empty(result.Items[0].PrerequisiteIds);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ContentJsonFormatter
```
Expected: FAIL — class not found.

- [ ] **Step 3: Implement ContentJsonFormatter**

Create `DataBuilder/Formatters/ContentJsonFormatter.cs`:

```csharp
using System.Collections.Generic;
using DataBuilder.Models;

namespace DataBuilder.Formatters;

public static class ContentJsonFormatter
{
    private static readonly Dictionary<string, int> ExpansionOrder = new()
    {
        ["ARR"] = 0, ["HW"] = 1, ["SB"] = 2, ["ShB"] = 3, ["EW"] = 4, ["DT"] = 5
    };

    private static readonly Dictionary<string, int> CategoryOrder = new()
    {
        ["SideQuest"] = 0, ["BlueUnlock"] = 1, ["JobQuest"] = 2, ["RoleQuest"] = 3,
        ["TrialSeries"] = 4, ["RaidSeries"] = 5, ["AllianceRaid"] = 6,
        ["BeastTribe"] = 7, ["CustomDelivery"] = 8
    };

    public static FormattedItemsFile Format(List<DetailItem> items)
    {
        var validItems = items.Where(i =>
            !string.IsNullOrWhiteSpace(i.Name) &&
            !string.IsNullOrWhiteSpace(i.Category) &&
            !string.IsNullOrWhiteSpace(i.Expansion)).ToList();

        var sorted = validItems
            .OrderBy(i => ExpansionOrder.GetValueOrDefault(i.Expansion, 99))
            .ThenBy(i => CategoryOrder.GetValueOrDefault(i.Category, 99))
            .ThenBy(i => i.Level ?? 0)
            .ThenBy(i => i.Name)
            .ToList();

        var nameToId = new Dictionary<string, uint>();
        var formattedItems = new List<FormattedItem>();
        uint nextId = 1;

        foreach (var item in sorted)
        {
            var formatted = new FormattedItem
            {
                Id = nextId,
                Name = item.Name,
                Level = item.Level ?? 0,
                Expansion = item.Expansion,
                Category = item.Category,
                LocationTerritoryId = item.LocationTerritoryId,
                LocationMapX = item.LocationMapX,
                LocationMapY = item.LocationMapY,
                QuestId = item.QuestId,
                AchievementId = item.AchievementId,
                WikiUrl = item.WikiUrl,
            };

            nameToId[item.Name] = nextId;
            formattedItems.Add(formatted);
            nextId++;
        }

        // Resolve prerequisite names to IDs
        for (var i = 0; i < sorted.Count; i++)
        {
            foreach (var prereqName in sorted[i].PrerequisiteNames)
            {
                if (nameToId.TryGetValue(prereqName, out var prereqId))
                    formattedItems[i].PrerequisiteIds.Add(prereqId);
                else
                    Console.Error.WriteLine($"WARN: Unresolvable prerequisite '{prereqName}' for '{sorted[i].Name}'");
            }
        }

        return new FormattedItemsFile { Version = 1, Items = formattedItems };
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj --filter ContentJsonFormatter
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DataBuilder/Formatters/ContentJsonFormatter.cs DataBuilder.Tests/Formatters/ContentJsonFormatterTests.cs
git commit -m "feat: add ContentJsonFormatter with ID assignment and prereq resolution"
```

---

### Task 12: Program.cs — CLI entry point and pipeline orchestration

**Files:**
- Modify: `DataBuilder/Program.cs`

- [ ] **Step 1: Implement Program.cs**

```csharp
using System.IO;
using System.Text.Json;
using DataBuilder.Formatters;
using DataBuilder.Models;
using DataBuilder.Scrapers;

namespace DataBuilder;

class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static async Task<int> Main(string[] args)
    {
        var fromStage = "scratch";
        var outputPath = "../FfxivTodo/Data/content.json";

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--from" && i + 1 < args.Length)
                fromStage = args[++i];
            if (args[i] == "--output" && i + 1 < args.Length)
                outputPath = args[++i];
        }

        var cacheDir = "Cache";
        Directory.CreateDirectory(cacheDir);

        var catFile = Path.Combine(cacheDir, "category_items.json");
        var detailFile = Path.Combine(cacheDir, "detail_items.json");
        var resolvedFile = Path.Combine(cacheDir, "resolved_items.json");

        List<CategoryItem> categoryItems;
        List<DetailItem> detailItems;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "FfxivTodo-DataBuilder/1.0");

        // Stage 1: Category scrape
        if (fromStage == "scratch")
        {
            Console.WriteLine("Stage 1: Scraping category pages...");
            var catScraper = new WikiCategoryScraper(http);
            categoryItems = await catScraper.ScrapeAllAsync();
            await File.WriteAllTextAsync(catFile, JsonSerializer.Serialize(
                new CategoryItemsFile { Items = categoryItems }, JsonOpts));
            Console.WriteLine($"  Found {categoryItems.Count} items.");
        }
        else
        {
            Console.WriteLine($"Stage 1: Loading from {catFile}...");
            var json = await File.ReadAllTextAsync(catFile);
            categoryItems = JsonSerializer.Deserialize<CategoryItemsFile>(json)?.Items
                           ?? new List<CategoryItem>();
        }

        // Stage 2: Detail scrape
        if (fromStage is "scratch" or "categories")
        {
            Console.WriteLine("Stage 2: Scraping detail pages...");
            var detailScraper = new WikiDetailScraper(http);
            detailItems = await detailScraper.ScrapeDetailsAsync(categoryItems);
            await File.WriteAllTextAsync(detailFile, JsonSerializer.Serialize(
                new DetailItemsFile { Items = detailItems }, JsonOpts));
            Console.WriteLine($"  Scraped {detailItems.Count} detail pages.");
        }
        else
        {
            Console.WriteLine($"Stage 2: Loading from {detailFile}...");
            var json = await File.ReadAllTextAsync(detailFile);
            detailItems = JsonSerializer.Deserialize<DetailItemsFile>(json)?.Items
                         ?? new List<DetailItem>();
        }

        // Stage 3: ID resolution
        if (fromStage is "scratch" or "categories" or "details")
        {
            Console.WriteLine("Stage 3: Resolving IDs via EDB + XIVAPI...");
            var resolver = new XivApiResolver(http);
            foreach (var item in detailItems)
                await resolver.ResolveAsync(item);

            await File.WriteAllTextAsync(resolvedFile, JsonSerializer.Serialize(
                new DetailItemsFile { Items = detailItems }, JsonOpts));
            Console.WriteLine("  IDs resolved.");
        }
        else
        {
            Console.WriteLine($"Stage 3: Loading from {resolvedFile}...");
            var json = await File.ReadAllTextAsync(resolvedFile);
            detailItems = JsonSerializer.Deserialize<DetailItemsFile>(json)?.Items
                         ?? new List<DetailItem>();
        }

        // Stage 4: Format and output
        Console.WriteLine("Stage 4: Formatting content.json...");
        var formatted = ContentJsonFormatter.Format(detailItems);
        var outputJson = JsonSerializer.Serialize(formatted, JsonOpts);
        await File.WriteAllTextAsync(outputPath, outputJson);

        Console.WriteLine($"Done! {formatted.Items.Count} items written to {outputPath}");
        return 0;
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build DataBuilder/DataBuilder.csproj
```
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add DataBuilder/Program.cs
git commit -m "feat: add Program.cs with CLI orchestration and pipeline"
```

---

### Task 13: Run all tests and verify end-to-end

**Files:** None

- [ ] **Step 1: Run all unit tests**

```bash
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj -v normal
```
Expected: All tests pass.

- [ ] **Step 2: Run DataBuilder with --help (dry run)**

```bash
dotnet run --project DataBuilder -- --from resolved --output /tmp/test_content.json
```
Expected: Loads from cache if exists, otherwise errors gracefully. Validates the CLI works.

- [ ] **Step 3: Final commit**

```bash
git add -A
git commit -m "complete: DataBuilder implementation with all pipeline stages"
```
