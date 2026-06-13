# DataBuilder — Design Spec

A C# console tool that scrapes, imports, and formats FFXIV content data into the `content.json` bundle used by the FfxivTodo Dalamud plugin.

## Overview

**Goal**: Automate generation of `content.json` from Consolegameswiki + XIVAPI data sources.

**Language**: C# (.NET 8+, console app). Same ecosystem as the plugin.

**Sources**:
- Consolegameswiki — names, categories, levels, expansions, prerequisites, wiki URLs, EDB links, location coordinates
- XIVAPI — Quest IDs (fallback), Achievement IDs, Territory IDs (fallback)

## Pipeline Architecture

```
┌──────────────────┐
│ 1. Category       │  Fetch category pages, extract lists of quest/content names
│    Scraper        │  Tables where available, link extraction where not
└────────┬─────────┘
         ▼  category_items.json
┌──────────────────┐
│ 2. Detail         │  For each name, fetch individual page → parse infobox
│    Scraper        │  Extracts: Level, Location+Coords, Prereqs, EDB link,
│                   │  Categories, Unlocks, WikiUrl
└────────┬─────────┘
         ▼  detail_items.json
┌──────────────────┐
│ 3. ID Resolver    │  From EDB links → extract numeric Quest IDs
│    + XIVAPI       │  XIVAPI for Achievement IDs, Territory IDs, fallbacks
└────────┬─────────┘
         ▼  resolved_items.json
┌──────────────────┐
│ 4. Formatter      │  Merge, assign sequential ContentItem IDs,
│                   │  resolve prereq names→IDs, output content.json
└──────────────────┘
```

**Files produced**:
- `category_items.json` — flat list of {name, category, expansion} per wiki page
- `detail_items.json` — category items enriched with infobox data
- `resolved_items.json` — detail items with numeric QuestId, AchievementId, TerritoryId
- `content.json` — final bundle matching the plugin schema (Item 1 below)

## Stage 1: Category Scraper

**Source pages by category:**

| Category | Wiki Page | Extraction Method |
|----------|-----------|-------------------|
| `JobQuest` | `Job_Quests` | Table rows by job subsection (`### Paladin Quests`, etc.) |
| `RoleQuest` | `Feature_Quests` § "Role Quests" (links to sub-pages per role) | Linked quest names, then individual page scrape |
| `RaidSeries` | `Raids` § "Normal Raids" | Table: "Unlock" column gives unlock quest. Then scrape unlock page → "Unlocks" section for duty names. |
| `AllianceRaid` | `Raids` § "Alliance Raids" | Same as RaidSeries — unlock quest → Unlocks section for raid names. |
| `TrialSeries` | `Raids` § individual trial pages, or `Trials` | Same pattern — unlock quest → duty names. |
| `BlueUnlock` | `Feature_Quests` subsections: Dungeons, Grand Company, The Hunt, Locations, PvP, Stone Sky Sea, Guildhests, Aether Current Quests, Side Story Questlines | Table rows from each subsection |
| `SideQuest` | `Side_Quests` | Table rows per zone/expansion |
| `BeastTribe` | `Allied_Society_Quests` | Prose: "To unlock the Amalj'aa quests players must complete the level 43 quest **Peace for Thanalan**". Parse unlock quest name, then individual page scrape. |
| `CustomDelivery` | `Custom_Deliveries` | Same as BeastTribe — unlock quest from prose, then individual page. |

**Expansion detection**: Section headers contain expansion names ("A Realm Reborn", "Heavensward", "Stormblood", "Shadowbringers", "Endwalker", "Dawntrail").

**Output schema** (`category_items.json`):
```json
{
  "items": [
    {
      "name": "Primal Awakening",
      "category": "RaidSeries",
      "expansion": "ARR"
    }
  ]
}
```

## Stage 2: Detail Scraper

For each item in `category_items.json`, fetch its individual wiki page (e.g., `https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening`).

**Infobox fields extracted**:

| Infobox Field | Maps To | Example |
|--------------|---------|---------|
| `Level` | `ContentItem.Level` | `50` |
| `Location` | `LocationTerritoryId` + `LocationMapX/Y` | `The Waking Sands (X:6.0, Y:4.9)` |
| `Requirements` | `PrerequisiteNames` | `The Navel (Hard) cleared` |
| `Unlocks` | Duty/system names (for type B items) | `The Binding Coil of Bahamut - Turn 1` etc. |
| `Next quest` | Chain continuation (informational) | `Alisaie's Pledge` |
| `Links → EDB` | EDB URL containing Quest ID | `…/lodestone/playguide/db/quest/65586/` |
| `Links → GT` | Garland Tools URL (fallback ID source) | — |
| Categories (bottom) | Classification validation | `Feature quests`, `Bahamut Quests` |
| Page URL | `WikiUrl` | `https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening` |

**Location parsing**: Regex extract territory name and `(X:##.#, Y:##.#)` coordinates from the Location field.

**Prerequisite parsing**: The Requirements field lists quest names that must be completed. Parse and store as a string array.

**Rate limiting**: 1 request/second (1000ms delay). With ~500-1000 items, a full run takes 8-16 minutes. Results cached incrementally so re-runs only fetch new/changed pages.

**Output schema** (`detail_items.json`):
```json
{
  "items": [
    {
      "name": "Primal Awakening",
      "category": "RaidSeries",
      "expansion": "ARR",
      "level": 50,
      "locationTerritoryName": "The Waking Sands",
      "locationMapX": 6.0,
      "locationMapY": 4.9,
      "prerequisiteNames": ["The Navel (Hard)"],
      "edbUrl": "https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/",
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening"
    }
  ]
}
```

### Item Type Distinction

The pipeline handles two types of content items:

**A. Quest-as-content** (JobQuest, RoleQuest, SideQuest, BlueUnlock, BeastTribe, CustomDelivery):
The quest page on the wiki IS the content item. Name, level, coords all come directly from that page's infobox. The item tracks the quest itself.

**B. Duty/system-as-content** (RaidSeries, AllianceRaid, TrialSeries):
The category page (Raids) lists duty names AND their unlock quest in separate table columns. The pipeline:
1. Get duty names and unlock quest names from the category table
2. Scrape the unlock quest's detail page
3. Create a ContentItem per duty, with:
   - `name` = duty name (from table column)
   - `questId` = unlock quest's ID (from EDB link on the unlock quest page)
   - `achievementId` = completion achievement (from XIVAPI)
   - `Location*` = from the unlock quest's infobox
   - `PrerequisiteIds` = from the unlock quest's "Requirements" field

**Example**: The `Raids` page table row:
```
The Binding Coil of Bahamut | 50 | 70-82 | 5 turns | Twintania | ... | Primal Awakening
```
- Duty name: "The Binding Coil of Bahamut" (from the "Duty Name" table column)
- Unlock quest name: "Primal Awakening" (from the "Unlock" column)
- Scrape `Primal_Awakening` page → questId=65586, coords=(6.0,4.9), prereq="The Navel (Hard)"
- Create one ContentItem: name="The Binding Coil of Bahamut", questId=65586

Note: The Raids table groups by coil/tier (e.g., "The Binding Coil of Bahamut" is one row, not 5). This matches the intended v1 granularity — track entire tiers/coils, not individual fights.

## Stage 3: ID Resolver + XIVAPI

**Quest ID extraction**:
- Primary: Parse `edbUrl` — regex extract numeric ID from lodestone URL
- Fallback: Query XIVAPI `/search?string={name}&indexes=quest`
- Cache: Store resolved IDs in local lookup so re-runs skip API calls

**Achievement ID lookup** (XIVAPI only):
- For duty-type items (RaidSeries, AllianceRaid, TrialSeries), derive achievement name from content name. Most follow the pattern `"Mapping the Realm: {name}"` but exceptions exist (e.g., "A Tankless Job").
- Try both the known pattern and a direct name search. Take whichever returns a result.
- Maintain a checked-in override file at `DataBuilder/achievement_overrides.json` for known exceptions. Schema:
```json
{
  "achievement_overrides.json": [
    { "contentName": "The Navel (Hard)", "achievementId": 123 },
    { "contentName": "The Howling Eye (Extreme)", "achievementId": 693 }
  ]
}
```
- Not all items have achievements — `achievementId` stays null when not found.

**Territory ID lookup**:
- From `locationTerritoryName` → query XIVAPI `/search?string={territory_name}&indexes=territorytype`
- Cache territory name→ID mappings

**Output schema** (`resolved_items.json`):
```json
{
  "items": [
    {
      "name": "Primal Awakening",
      "category": "RaidSeries",
      "expansion": "ARR",
      "level": 50,
      "prerequisiteNames": ["The Navel (Hard)"],
      "locationTerritoryId": 103,
      "locationMapX": 6.0,
      "locationMapY": 4.9,
      "questId": 65586,
      "achievementId": 690,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening"
    }
  ]
}
```

## Stage 4: Formatter

**Operations**:
1. Assign sequential `id` values (1, 2, 3...)
2. Map `prerequisiteNames` to assigned IDs — warn on unresolvable names
3. Remove items missing required fields (name, category, expansion)
4. Sort by expansion → category → level → name
5. Output to `content.json` matching the plugin schema

**Output schema** (`content.json`):
```json
{
  "version": 1,
  "items": [
    {
      "id": 1,
      "name": "Primal Awakening",
      "level": 50,
      "expansion": "ARR",
      "category": "RaidSeries",
      "prerequisiteIds": [2],
      "locationTerritoryId": 103,
      "locationMapX": 6.0,
      "locationMapY": 4.9,
      "questId": 65586,
      "achievementId": 690,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening"
    }
  ]
}
```

Note that duty content (RaidSeries, AllianceRaid, TrialSeries) is sourced from the wiki table's "Duty Name" column, not from the unlock quest's "Unlocks" section. The unlock quest is scraped only for questId, coordinates, and prerequisites.

## Project Structure

```
ffxiv-todo/
├── FfxivTodo/                    (existing C# Dalamud plugin)
│   └── Data/
│       └── content.json           ← output target for formatter
├── DataBuilder/                   (new C# console tool)
│   ├── DataBuilder.csproj
│   ├── Program.cs                 (orchestrator, CLI args)
│   ├── Scrapers/
│   │   ├── WikiCategoryScraper.cs
│   │   ├── WikiDetailScraper.cs
│   │   └── XivApiResolver.cs
│   ├── Formatters/
│   │   └── ContentJsonFormatter.cs
│   ├── Models/
│   │   └── PipelineModels.cs      (intermediate DTOs)
│   └── Cache/                     (intermediate JSON files)
└── ffxiv-todo.sln
```

## CLI

```
dotnet run --project DataBuilder -- --from scratch          # Full run
dotnet run --project DataBuilder -- --from categories       # Resume from category scrape
dotnet run --project DataBuilder -- --from details          # Resume from detail scrape
dotnet run --project DataBuilder -- --from resolved         # Resume from resolved data
dotnet run --project DataBuilder -- --output ../FfxivTodo/Data/content.json
```

## Dependencies

- `HtmlAgilityPack` — HTML parsing for wiki pages
- `System.Text.Json` — JSON serialization (built-in)
- `Microsoft.Extensions.Http` — HttpClient with DI and resilience policies

## Error Handling

- Wiki HTTP failures: retry up to 3 times with exponential backoff
- XIVAPI failures: log warning, leave field null, continue with remaining items
- Missing required fields: log error, skip item
- Unresolvable prerequisite names: log warning, omit from prerequisiteIds array

## Out of Scope

- GUI or web interface for the tool
- Incremental/patch-only data updates (full regeneration each run)
- Automated scheduling (manual run or CI-triggered)
