# Additional Content Types Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 7 missing content types (Role Quests, Variant & Criterion Dungeons, Relic Weapons, Island Sanctuary, Faux Hollows, The Masked Carnivale, Ishgardian Restoration) to the data pipeline — ~35 new items with wiki-scraped data, quest chain linking, and achievement tracking.

**Architecture:** Follow existing pipeline: wiki scrapers produce CategoryItems → CSV enrichment → quest chain resolution → format to content.json. New parsers in WikiCategoryScraper.cs produce named CategoryItems. Quest chain overrides in quest_chain_overrides.json link content items to BlueUnlock quest IDs. Achievement overrides in achievement_overrides.json provide alternate completion detection for content types that use achievements instead of quests.

**Tech Stack:** C#, HtmlAgilityPack, System.Text.Json. Existing pipeline uses `DataBuilder` console app → `FfxivTodo/Data/content.json`.

---

### Task 1: Add new ContentCategory enum values

**Files:**
- Modify: `FfxivTodo/Models/Enums.cs:18-35`

- [ ] **Step 1: Add 5 new enum values after DeepDungeon**

```csharp
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
    CustomDelivery,
    SavageRaid,
    UltimateRaid,
    FieldOperation,
    VariantDungeon,
    ChaoticRaid,
    DeepDungeon,
    RelicWeapon,
    IslandSanctuary,
    IshgardianRestoration,
    FauxHollows,
    MaskedCarnivale
}
```

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Models/Enums.cs
git commit -m "feat: add RelicWeapon, IslandSanctuary, IshgardianRestoration, FauxHollows, MaskedCarnivale enum values"
```

---

### Task 2: Add category display labels

**Files:**
- Modify: `FfxivTodo/Windows/MainWindowFilterLogic.cs:30-48`

- [ ] **Step 1: Add 5 new GetCategoryLabel entries after DeepDungeon**

```csharp
ContentCategory.RelicWeapon => "Relic weapons",
ContentCategory.IslandSanctuary => "Island Sanctuary",
ContentCategory.IshgardianRestoration => "Ishgardian Restoration",
ContentCategory.FauxHollows => "Faux Hollows",
ContentCategory.MaskedCarnivale => "The Masked Carnivale",
```

Place these inside the existing switch expression, after the `ContentCategory.DeepDungeon` line and before `_ => category.ToString()`.

- [ ] **Step 2: Commit**

```bash
git add FfxivTodo/Windows/MainWindowFilterLogic.cs
git commit -m "feat: add category labels for new content types"
```

---

### Task 3: Add CategoryOrder entries in formatter

**Files:**
- Modify: `DataBuilder/Formatters/ContentJsonFormatter.cs:16-23`

- [ ] **Step 1: Add 5 new CategoryOrder entries**

```csharp
private static readonly Dictionary<string, int> CategoryOrder = new(StringComparer.OrdinalIgnoreCase)
{
    // ... existing entries ...
    ["DeepDungeon"] = 14,
    ["RelicWeapon"] = 15,
    ["IslandSanctuary"] = 16,
    ["IshgardianRestoration"] = 17,
    ["FauxHollows"] = 18,
    ["MaskedCarnivale"] = 19,
};
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Formatters/ContentJsonFormatter.cs
git commit -m "feat: add CategoryOrder for new content types"
```

---

### Task 4: Add ParseRoleQuestsPage scraper

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add the parser method after the existing ParseDeepDungeonsPage method**

The `/wiki/Role_Quests` page has structure:
```
h2: Shadowbringers
  h3: Tank (table with 7 quests)
  h3: Physical DPS (table with 6 quests)
  h3: Magical Ranged DPS (table with 6 quests)
  h3: Healer (table with 6 quests)
  h3: Master Role Quests (table with 2 quests)
h2: Endwalker
  h3: Tank (table with 6 quests)
  ... (5 roles + Master)
h2: Dawntrail
  h3: Tank (table with 6 quests)
  ... (5 roles + Master)
```

```csharp
public List<CategoryItem> ParseRoleQuestsPage(HtmlNode contentNode)
{
    var items = new List<CategoryItem>();
    var currentExpansion = string.Empty;
    var currentRole = string.Empty;

    var headings = contentNode.SelectNodes(".//h2|.//h3");
    if (headings == null) return items;

    foreach (var heading in headings)
    {
        var sectionId = GetHeadingId(heading);
        if (string.IsNullOrEmpty(sectionId)) continue;

        // Try as expansion heading first (h2)
        var cleaned = Regex.Replace(sectionId.Replace("_", " "), @"^\d+\s+", "");
        var exp = ParseExpansionFromHeading(cleaned);
        if (exp != null)
        {
            currentExpansion = exp;
            currentRole = string.Empty;
            continue;
        }

        // h3: role name
        if (sectionId.Equals("Master_Role_Quests", StringComparison.OrdinalIgnoreCase))
        {
            currentRole = "Master";
        }
        else
        {
            currentRole = sectionId.Replace("_", " ");
        }

        // Parse the quest names from the table
        var rows = new List<string>();
        var table = FindNextTable(heading);
        if (table != null)
        {
            var trs = table.SelectNodes(".//tr");
            if (trs != null)
            {
                foreach (var row in trs.Skip(1))
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 1) continue;
                    var link = cells[0].SelectSingleNode(".//a");
                    if (link != null)
                    {
                        rows.Add(HttpUtility.HtmlDecode(link.InnerText.Trim()));
                    }
                }
            }
        }

        // Produce one content item for this role chain
        var itemName = $"{currentExpansion} {currentRole} Role Quests";
        items.Add(new CategoryItem
        {
            Name = itemName,
            Category = "RoleQuest",
            Expansion = currentExpansion
        });
    }

    return items;
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add ParseRoleQuestsPage scraper"
```

---

### Task 5: Add ParseVariantDungeonsPage scraper

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add the parser method**

The `/wiki/Variant_and_Criterion_Dungeons` page has tables in two sections: "Available Variant Dungeons" and "Available Criterion Dungeons".

```csharp
public List<CategoryItem> ParseVariantDungeonsPage(HtmlNode contentNode)
{
    var items = new List<CategoryItem>();
    var currentSection = string.Empty;

    var headings = contentNode.SelectNodes(".//h2|.//h3");
    if (headings == null) return items;

    foreach (var heading in headings)
    {
        var sectionId = GetHeadingId(heading);
        if (string.IsNullOrEmpty(sectionId)) continue;

        if (sectionId.Contains("Variant_Dungeons") && !sectionId.Contains("Criterion"))
        {
            currentSection = "VariantDungeon";
            // Find the "Available Variant Dungeons" sub-heading table
            continue;
        }
        if (sectionId.Contains("Criterion_Dungeons") && !sectionId.Contains("Savage"))
        {
            currentSection = "VariantDungeon";
            // Criterion entries have "Another" prefix
            continue;
        }
        if (sectionId.Contains("Advanced") || sectionId.Contains("Savage"))
        {
            currentSection = string.Empty;
            continue;
        }

        // Parse tables under Variant or Criterion sections
        if (!string.IsNullOrEmpty(currentSection))
        {
            var table = FindNextTable(heading);
            if (table != null)
            {
                var rows = table.SelectNodes(".//tr");
                if (rows != null)
                {
                    foreach (var row in rows.Skip(1))
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells == null || cells.Count < 1) continue;
                        var link = cells[0].SelectSingleNode(".//a");
                        if (link == null) continue;
                        var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        items.Add(new CategoryItem
                        {
                            Name = name,
                            Category = "VariantDungeon",
                            Expansion = "EW" // default, refined during enrichment
                        });
                    }
                }
            }
        }
    }

    return items;
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add ParseVariantDungeonsPage scraper"
```

---

### Task 6: Add ParseIslandSanctuaryPage scraper

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add the parser method**

The island sanctuary page has a "Questline" section with a table listing 6 quests.

```csharp
public List<CategoryItem> ParseIslandSanctuaryPage(HtmlNode contentNode)
{
    var items = new List<CategoryItem>();

    // Find the heading for questline and its table
    var headings = contentNode.SelectNodes(".//h2|.//h3");
    if (headings != null)
    {
        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (sectionId != "Questline") continue;

            var table = FindNextTable(heading);
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
                if (!string.IsNullOrWhiteSpace(name))
                {
                    items.Add(new CategoryItem
                    {
                        Name = name,
                        Category = "BlueUnlock",
                        Expansion = "EW"
                    });
                }
            }
        }
    }

    // Add the content item itself
    items.Add(new CategoryItem
    {
        Name = "Island Sanctuary",
        Category = "IslandSanctuary",
        Expansion = "EW"
    });

    return items;
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add ParseIslandSanctuaryPage scraper"
```

---

### Task 7: Add ParseFauxHollowsPage scraper

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add the parser method**

The Faux Hollows page has the unlock quest name in a paragraph and 4 achievements in a table. We just need to create the content item — the unlock quest comes from the Feature Quests page and achievement IDs from overrides.

```csharp
public List<CategoryItem> ParseFauxHollowsPage(HtmlNode contentNode)
{
    return new List<CategoryItem>
    {
        new()
        {
            Name = "Faux Hollows",
            Category = "FauxHollows",
            Expansion = "ShB"
        }
    };
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add ParseFauxHollowsPage scraper"
```

---

### Task 8: Add ParseMaskedCarnivalePage scraper

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add the parser method**

```csharp
public List<CategoryItem> ParseMaskedCarnivalePage(HtmlNode contentNode)
{
    return new List<CategoryItem>
    {
        new()
        {
            Name = "The Masked Carnivale",
            Category = "MaskedCarnivale",
            Expansion = "SB"
        }
    };
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add ParseMaskedCarnivalePage scraper"
```

---

### Task 9: Add ParseIshgardianRestorationPage scraper

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add the parser method**

```csharp
public List<CategoryItem> ParseIshgardianRestorationPage(HtmlNode contentNode)
{
    return new List<CategoryItem>
    {
        new()
        {
            Name = "Ishgardian Restoration",
            Category = "IshgardianRestoration",
            Expansion = "ShB"
        }
    };
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add ParseIshgardianRestorationPage scraper"
```

---

### Task 10: Add static Relic Weapons items

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add new method

- [ ] **Step 1: Add a static method returning the 7 relic weapon series**

Relic weapons don't appear as tables on the Feature Quests page (they're paragraph links), and a dedicated page parser would be overkill. Define them statically.

```csharp
private static List<CategoryItem> GetStaticRelicWeapons()
{
    return new List<CategoryItem>
    {
        new() { Name = "Zodiac Weapons",      Category = "RelicWeapon", Expansion = "ARR" },
        new() { Name = "Anima Weapons",       Category = "RelicWeapon", Expansion = "HW"  },
        new() { Name = "Eureka Weapons",      Category = "RelicWeapon", Expansion = "SB"  },
        new() { Name = "Resistance Weapons",  Category = "RelicWeapon", Expansion = "ShB" },
        new() { Name = "Skysteel Tools",      Category = "RelicWeapon", Expansion = "ShB" },
        new() { Name = "Manderville Weapons", Category = "RelicWeapon", Expansion = "EW"  },
        new() { Name = "Phantom Weapons",     Category = "RelicWeapon", Expansion = "DT"  },
    };
}
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: add GetStaticRelicWeapons helper"
```

---

### Task 11: Modify ParseFeatureQuestsPage to un-skip sections

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs:335-365` (ParseFeatureQuestsPage)

- [ ] **Step 1: Un-skip Records of Unusual Endeavors**

In `ParseFeatureQuestsPage`, change line 349-350:
```csharp
// BEFORE:
if (sectionId.Contains("Records_of_Unusual"))
{ currentCategory = string.Empty; continue; }

// AFTER:
if (sectionId.Contains("Records_of_Unusual"))
{ currentCategory = "BlueUnlock"; continue; }
```

- [ ] **Step 2: Un-skip Relic Weapons section**

Change lines 357-361:
```csharp
// BEFORE:
if (sectionId.Contains("Relic_Weapons")
    || sectionId.Contains("The_Forbidden_Land")
    || sectionId.Contains("Save_the_Queen")
    || sectionId.Contains("Occult_Crescent"))
{ currentCategory = string.Empty; continue; }

// AFTER:
// Allow relic weapon sub-sections to produce BlueUnlock items
if (sectionId.Contains("Relic_Weapons"))
{ /* keep currentCategory from parent (BlueUnlock) */ continue; }

// The Forbidden Land, Save the Queen, Occult Crescent all have quest tables
if (sectionId.Contains("The_Forbidden_Land")
    || sectionId.Contains("Save_the_Queen")
    || sectionId.Contains("Occult_Crescent"))
{ /* keep currentCategory from parent (BlueUnlock) */ continue; }
```

- [ ] **Step 3: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: un-skip Records_of_Unusual and Relic_Weapons in Feature Quests parser"
```

---

### Task 12: Wire new parsers into ScrapeAllAsync

**Files:**
- Modify: `DataBuilder/Scrapers/WikiCategoryScraper.cs:565-611` (ScrapeAllAsync)

- [ ] **Step 1: Add new scraper calls after the Custom Deliveries block**

After the Custom Deliveries try/catch block (line ~608), add:

```csharp
// Role Quests
try
{
    var roleItems = await FetchAndParseAsync("/wiki/Role_Quests", doc =>
        ParseRoleQuestsPage(doc.DocumentNode));
    allItems.AddRange(roleItems);
}
catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Role Quests: {ex.Message}"); }

// Variant & Criterion Dungeons
try
{
    var variantItems = await FetchAndParseAsync("/wiki/Variant_and_Criterion_Dungeons", doc =>
        ParseVariantDungeonsPage(doc.DocumentNode));
    allItems.AddRange(variantItems);
}
catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Variant Dungeons: {ex.Message}"); }

// Island Sanctuary
try
{
    var islandItems = await FetchAndParseAsync("/wiki/Island_Sanctuary", doc =>
        ParseIslandSanctuaryPage(doc.DocumentNode));
    allItems.AddRange(islandItems);
}
catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Island Sanctuary: {ex.Message}"); }

// Faux Hollows
try
{
    var fauxItems = await FetchAndParseAsync("/wiki/Faux_Hollows", doc =>
        ParseFauxHollowsPage(doc.DocumentNode));
    allItems.AddRange(fauxItems);
}
catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Faux Hollows: {ex.Message}"); }

// The Masked Carnivale
try
{
    var carnItems = await FetchAndParseAsync("/wiki/The_Masked_Carnivale", doc =>
        ParseMaskedCarnivalePage(doc.DocumentNode));
    allItems.AddRange(carnItems);
}
catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Masked Carnivale: {ex.Message}"); }

// Ishgardian Restoration
try
{
    var restoItems = await FetchAndParseAsync("/wiki/Ishgardian_Restoration", doc =>
        ParseIshgardianRestorationPage(doc.DocumentNode));
    allItems.AddRange(restoItems);
}
catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Ishgardian Restoration: {ex.Message}"); }

// Relic Weapons (static)
allItems.AddRange(GetStaticRelicWeapons());
```

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Scrapers/WikiCategoryScraper.cs
git commit -m "feat: wire new parsers into ScrapeAllAsync"
```

---

### Task 13: Add Role Quest chain overrides

**Files:**
- Modify: `DataBuilder/Data/quest_chain_overrides.json`

- [ ] **Step 1: Look up role quest IDs from CSV**

Run this to find quest IDs for the first quest in each role chain:

```bash
python3 -c "
import csv
quests = {}
with open('Cache/csv/Quest.csv', 'r') as f:
    reader = csv.reader(f)
    next(reader)
    for row in reader:
        name = row[1].strip()
        if name:
            quests[name.lower()] = int(row[0])

# Role quest chain starters
names = [
    # ShB
    'The Man with Too Many Scars',   # Tank
    'No Greater Sport',              # Physical DPS
    'Hollow Pursuits',               # Magical Ranged DPS
    'Traditions and Travails',       # Healer
    'Shadow Walk with Me',           # Master
    # EW
    'Shrouded in Peril',             # Tank
    'Storm Clouds Brewing',          # Melee DPS
    'Seeds of Disquiet',             # Physical Ranged DPS
    'Our Aching Souls',              # Magical Ranged DPS
    'Far from Free',                 # Healer
    'Bitter Snow',                   # Master
    # DT
    'The Narwhal Beckons',           # Tank
    'The Hunter and the Hunted',     # Melee DPS
    'To Steal a Steelhog',           # Physical Ranged DPS
    'Power Forgotten',               # Magical Ranged DPS
    'In the Sting of Things',        # Healer
    'Picking Up the Torch',          # Master
]
for name in names:
    qid = quests.get(name.lower())
    if qid:
        print(f'{name}: {qid}')
    else:
        print(f'{name}: NOT FOUND')
"
```

Note: If CSV is not yet downloaded, run the DataBuilder stage 1 first to refresh caches.

- [ ] **Step 2: Add override entries to quest_chain_overrides.json**

Using the quest IDs from the CSV lookup, add entries to `quest_chain_overrides.json`:

```json
// Shadowbringers Role Quests
{ "contentName": "ShB Tank Role Quests",               "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "ShB Physical DPS Role Quests",        "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "ShB Magical Ranged DPS Role Quests",  "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "ShB Healer Role Quests",              "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "ShB Master Role Quests",              "questIds": [<first_quest_id>], "explicitChain": false },
// Endwalker Role Quests
{ "contentName": "EW Tank Role Quests",                 "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "EW Melee DPS Role Quests",            "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "EW Physical Ranged DPS Role Quests",  "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "EW Magical Ranged DPS Role Quests",   "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "EW Healer Role Quests",               "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "EW Master Role Quests",               "questIds": [<first_quest_id>], "explicitChain": false },
// Dawntrail Role Quests
{ "contentName": "DT Tank Role Quests",                 "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "DT Melee DPS Role Quests",            "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "DT Physical Ranged DPS Role Quests",  "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "DT Magical Ranged DPS Role Quests",   "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "DT Healer Role Quests",               "questIds": [<first_quest_id>], "explicitChain": false },
{ "contentName": "DT Master Role Quests",               "questIds": [<first_quest_id>], "explicitChain": false },
```

**Note:** Content names use expansion abbreviations (ShB, EW, DT) because the RoleQuests scraper sets `Expansion` with short codes, not "Shadowbringers" etc.

- [ ] **Step 3: Add Relic Weapon override entries**

```json
// Relic Weapons (first quest in each chain, non-explicit to walk prerequisites)
{ "contentName": "Zodiac Weapons",      "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Anima Weapons",       "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Eureka Weapons",      "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Resistance Weapons",  "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Skysteel Tools",      "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Manderville Weapons", "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Phantom Weapons",     "questIds": [<quest_id>], "explicitChain": false },
```

- [ ] **Step 4: Add single-entry content override entries**

```json
// Island Sanctuary, Faux Hollows, etc. — single unlock quest entries
{ "contentName": "Island Sanctuary",        "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Faux Hollows",            "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "The Masked Carnivale",    "questIds": [<quest_id>], "explicitChain": false },
{ "contentName": "Ishgardian Restoration",  "questIds": [<quest_id>], "explicitChain": false },
```

- [ ] **Step 5: Update ResolveWithWikiAsync to include RoleQuest in duty categories**

In `UnlockQuestResolver.cs:181-184`, add RoleQuest to the duty categories so wiki scraping can find unlock quest names:

```csharp
var dutyCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "RaidSeries", "TrialSeries", "AllianceRaid", "RoleQuest"
};
```

- [ ] **Step 6: Commit**

```bash
git add DataBuilder/Data/quest_chain_overrides.json DataBuilder/Scrapers/UnlockQuestResolver.cs
git commit -m "feat: add quest chain overrides for Role Quests, Relic Weapons, and single-entry content"
```

---

### Task 14: Add achievement overrides for new content

**Files:**
- Modify: `DataBuilder/Data/achievement_overrides.json`

- [ ] **Step 1: Add achievement override entries**

The Masked Carnivale and Faux Hollows have achievements that can signal completion.

```json
// Faux Hollows achievements (Friend or Faux)
{ "contentName": "Faux Hollows", "achievementId": 2634 },   // Friend or Faux I
// The Masked Carnivale achievements
{ "contentName": "The Masked Carnivale", "achievementId": <first_carnivale_achievement_id> },
// Ishgardian Restoration achievements (Skybuilder)
{ "contentName": "Ishgardian Restoration", "achievementId": <relevant_achievement_id> },
```

Note: Faux Hollows achievement 2634 (Friend or Faux I) triggers on first play. For completion tracking, 2637 (Friend or Faux IV - 50 plays) might be more appropriate as the "completion" signal. Decide per content type what constitutes "completion."

- [ ] **Step 2: Commit**

```bash
git add DataBuilder/Data/achievement_overrides.json
git commit -m "feat: add achievement overrides for new content types"
```

---

### Task 15: Build and run the DataBuilder pipeline

**Files:**
- None directly modified (pipeline output)

- [ ] **Step 1: Build the DataBuilder**

```bash
dotnet build DataBuilder/DataBuilder.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Run the full pipeline**

```bash
dotnet run --project DataBuilder/DataBuilder.csproj -- --output FfxivTodo/Data/content.json
```

Expected: Pipeline runs all stages. Watch for warnings about unmatched items or scrapers that hit errors.

- [ ] **Step 3: Verify item counts**

```bash
python3 -c "
import json
with open('FfxivTodo/Data/content.json') as f:
    data = json.load(f)
cats = {}
for item in data['items']:
    cat = item['category']
    cats[cat] = cats.get(cat, 0) + 1
for c, n in sorted(cats.items()):
    print(f'{c}: {n}')
print(f'\nTotal: {len(data[\"items\"])}')
"
```

Expected: New categories appear with appropriate counts:
- RoleQuest: ~17
- VariantDungeon: ~8-10 (expanded from 1 to ~9-11)
- RelicWeapon: 7
- IslandSanctuary: 1
- FauxHollows: 1
- MaskedCarnivale: 1
- IshgardianRestoration: 1
- Total: ~715

- [ ] **Step 4: Check specific items for correctness**

```bash
python3 -c "
import json
with open('FfxivTodo/Data/content.json') as f:
    data = json.load(f)
# Check Role Quest items exist
print('Role Quests:')
for item in data['items']:
    if item['category'] == 'RoleQuest':
        print(f'  {item[\"name\"]} ({item[\"expansion\"]}) - quests: {item[\"unlockQuestIds\"]}')
print()
# Check Relic Weapons  
print('Relic Weapons:')
for item in data['items']:
    if item['category'] == 'RelicWeapon':
        print(f'  {item[\"name\"]} ({item[\"expansion\"]}) - quests: {item[\"unlockQuestIds\"]}')
print()
# Check new single-entry items
for cat in ['IslandSanctuary', 'FauxHollows', 'MaskedCarnivale', 'IshgardianRestoration']:
    items = [i for i in data['items'] if i['category'] == cat]
    for item in items:
        print(f'{cat}: {item[\"name\"]} ({item[\"expansion\"]}) - quests: {item[\"unlockQuestIds\"]}')
"
```

- [ ] **Step 5: Commit generated content**

```bash
git add FfxivTodo/Data/content.json
git commit -m "feat: regenerate content.json with new content types (~715 items)"
```

---

### Task 16: Build and test the plugin

**Files:**
- None (verification only)

- [ ] **Step 1: Build the plugin**

```bash
dotnet build
```

Expected: Build succeeds. New enum values should be recognized.

- [ ] **Step 2: Verify filter labels work**

Check that `MainWindowFilterLogic.GetCategoryLabel` compiles and produces human-readable names for all new categories.

- [ ] **Step 3: Run lint and typecheck (if available)**

```bash
dotnet format --verify-no-changes 2>/dev/null || echo "No dotnet-format available"
```

- [ ] **Step 4: Commit any fixes**

```bash
git commit -am "chore: fix any build/lint issues from new content types"
```

---
