# CSV Integration Design

## Overview

Replace Stages 2 (wiki detail scraping) and 3 (XIVAPI resolution) of the DataBuilder pipeline with lookups against structured CSV data from [xivapi/ffxiv-datamining](https://github.com/xivapi/ffxiv-datamining), which publishes extracted FFXIV game data files in English.

Stage 1 (wiki category page scraping) is retained for its hand-curated category lists. Stage 4 (formatting/output) is unchanged.

## Motivation

- XIVAPI's search endpoint returns 500 errors consistently — no quest/achievement/territory IDs are resolved.
- Wiki detail page scraping requires 34+ sequential HTTP fetches per run, takes ~60s, and 2-3 pages consistently 404.
- Wiki infobox HTML parsing is fragile and misses data (e.g., Beast Tribe unlock quests lack standard infobox).
- CSV data is comprehensive, structured, and authoritative — it IS the game's data.
- The xivapi/ffxiv-datamining repo is actively maintained (last commit 2026-06-12).

## Architecture Changes

### Before (current)

```
Stage 1: WikiCategoryScraper → CategoryItem[] (527 items, ~5s)
Stage 2: WikiDetailScraper   → DetailItem[]   (10 concurrent fetches, ~60s)
Stage 3: XivApiResolver      → DetailItem[]   (mutated, 500 errors)
Stage 4: ContentJsonFormatter → content.json  (~488 items)
```

### After (proposed)

```
Stage 1: WikiCategoryScraper  → CategoryItem[]  (527 items, ~5s, unchanged)
Stage 2: CsvEnricher          → DetailItem[]    (<2s, no HTTP)
Stage 4: ContentJsonFormatter → content.json    (~488 items, unchanged)
```

Stage 3 is removed. All IDs (quest, achievement, territory) are populated during Stage 2 via CSV lookups.

## New Component: CsvDataProvider

Single class responsible for CSV download, caching, parsing, and lookups.

### Public API

```csharp
class CsvDataProvider
{
    CsvDataProvider(HttpClient http);
    
    // Download & parse all CSVs. Uses cached files if <30 days old.
    // Pass forceRefresh=true to re-download regardless.
    Task InitializeAsync(string cacheDir, bool forceRefresh = false);
    
    // Quest lookup by name (case-insensitive, normalized)
    QuestCsvRow? LookupQuest(string name);
    
    // Resolve foreign keys to names
    string? ResolveNpcName(int npcId);
    string? ResolveTerritoryName(int territoryId);
    string? ResolvePlaceName(int placeNameId);
    string? ResolveQuestName(int questId);  // for prerequisite chains
    
    // Achievement lookup (deferred: not populated yet)
    AchievementCsvRow? LookupAchievement(string name);
}
```

### Internal State

Each CSV is parsed into a dictionary keyed by the primary lookup field:

| Dictionary | Key | Value | Source CSV |
|---|---|---|---|
| `_questByName` | normalized quest name | `QuestCsvRow` | `en/Quest.csv` (34MB) |
| `_questById` | quest ID | `QuestCsvRow` | `en/Quest.csv` |
| `_npcNames` | NPC row ID | `string` (singular name) | `en/ENpcResident.csv` (~2MB) |
| `_territoryNames` | territory ID | `string` (name) | `en/TerritoryType.csv` (277KB) |
| `_placeNames` | place name ID | `string` (name) | `en/PlaceName.csv` (~200KB) |

### CSV Format

English CSV files from `https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/en/`.

They use the XIVData Oxidizer schema with column names as headers. Key columns in Quest.csv:

| Column | Index | Type | Content |
|---|---|---|---|
| `#` | 0 | int | Quest ID (matches XIVAPI quest IDs) |
| `Name` | 1 | string | English quest name |
| `ClassJobLevel[0]` | 1606 | int | Level requirement |
| `Expansion` | 1617 | int | 0=ARR, 1=HW, 2=SB, 3=ShB, 4=EW, 5=DT |
| `IssuerStart` | 1599 | int | NPC row ID → ENpcResident |
| `IssuerLocation` | 1600 | int | Territory ID → TerritoryType |
| `PlaceName` | 1615 | int | Place name ID → PlaceName |
| `PreviousQuest[0]` | 1591 | int | Prerequisite quest ID |
| `PreviousQuest[1]` | 1592 | int | Prerequisite quest ID |
| `PreviousQuest[2]` | 1593 | int | Prerequisite quest ID |
| `InstanceContentUnlock` | 1533 | int | Duty unlocked |
| `ClassJobUnlock` | 1626 | int | Job unlocked |
| `LevelMax` | 1641 | int | Maximum level (0 if no sync) |

### Caching Strategy

- CSVs are downloaded on first run from GitHub raw URLs
- Cached as-is in `Cache/csv/` directory with a `.etag` or timestamp file
- If cache is < 30 days old, skip download
- `--refresh-csvs` CLI flag forces re-download regardless of age
- If download fails and no cache exists → fatal error
- If download fails but cache exists → use cached copy, log warning
- CSV parsing errors (e.g., schema change) → delete cache, re-download
- Cache invalidation: check header row matches expected column names

## Name Matching Strategy

Wiki quest names and CSV quest names may differ. Resolution approach, tried in order:

1. **Exact match** — case-insensitive comparison after normalizing whitespace
2. **Remove parentheticals** — "Mining (Miner)" → "Mining"
3. **Smart quote normalization** — `\u2018\u2019\u201c\u201d` → straight quotes
4. **Match failure** → log warning, skip item (same behavior as current 404s)

A `NameOverrides.json` file provides manual mappings for persistent mismatches. Format:
```json
{
  "overrides": [
    { "wikiName": "Forward, Royal Marines", "csvName": "Forward, the Royal Marines" }
  ]
}
```

## Stage 2 Rewrite: CsvEnricher

Replaces `WikiDetailScraper`. Input: `List<CategoryItem>` from Stage 1. Output: `List<DetailItem>`.

### Processing per CategoryItem

```
CategoryItem → CsvDataProvider.LookupQuest(item.Name)
    ├── not found → log warning, skip
    └── found → build DetailItem:
        ├── Level = row.ClassJobLevel
        ├── QuestId = row.Id
        ├── Expansion = ExVersion name from row.Expansion (override Stage 1)
        ├── LocationTerritoryId = resolve IssuerLocation → territory ID
        ├── LocationTerritoryName = resolve IssuerLocation → territory name
        ├── IssuerName = resolve IssuerStart → NPC name
        ├── PrerequisiteNames = resolve PreviousQuest[0-2] → quest names
        ├── WikiUrl = construct from name (same as current)
        └── EdbUrl = construct from quest ID
```

### Parallelism

Not needed — CSV lookups are in-memory dictionary operations. A single-threaded linear scan of 527 items completes in <2s. No HTTP calls, no semaphore needed.

## Stage 3: Removed

`XivApiResolver` is deleted. The `--skip-id-resolution` CLI flag is retained as a no-op for backward compatibility (it will log a deprecation notice).

All IDs (QuestId, AchievementId, LocationTerritoryId) are populated by Stage 2.

AchievementId remains `null` for now — Achievement.csv is downloaded but not yet wired up. This matches current behavior (XIVAPI wasn't returning achievement IDs either).

## Files Changed

| File | Change |
|---|---|
| `DataBuilder/Program.cs` | Remove Stage 3 call, wire Stage 2 → CsvEnricher instead of WikiDetailScraper |
| `DataBuilder/Data/CsvDataProvider.cs` | **New** — CSV download, cache, parse, lookup |
| `DataBuilder/Data/CsvModels.cs` | **New** — `QuestCsvRow`, `AchievementCsvRow` POCOs |
| `DataBuilder/Scrapers/CsvEnricher.cs` | **New** — Stage 2 replacement |
| `DataBuilder/Scrapers/XivApiResolver.cs` | **Deleted** |
| `DataBuilder/Scrapers/WikiDetailScraper.cs` | **Kept** — available as fallback/deprecated |
| `DataBuilder.Tests/CsvDataProviderTests.cs` | **New** — unit tests |
| `DataBuilder.Tests/CsvEnricherTests.cs` | **New** — unit tests |
| `Cache/name_overrides.json` | **New** — manual name mappings |

## Testing Strategy

### Unit Tests (CsvDataProvider)

- Parse well-formed CSV rows correctly
- Lookup by name: exact match, case-insensitive, whitespace variations
- Handle missing NPC/territory/place IDs gracefully (return null)
- Handle empty CSVs (no header-only file)
- Handle CSV with BOM

### Integration Tests (CsvEnricher)

- Enrich a known CategoryItem against real cached CSV data
- Verify Level, QuestId, Expansion populated correctly
- Verify prerequisite chain resolution
- Verify name matching fallbacks work for known deviations

### Fixtures

- Small hand-crafted CSV files in `DataBuilder.Tests/TestData/csv/`
- Mimic the xivapi format with a few real quest rows

## Rollback Plan

`WikiDetailScraper` is kept in the codebase. A CLI flag `--legacy-stage2` reinvokes it instead of `CsvEnricher`. This allows fallback if CSV data has issues.

## Non-Goals

- Replacing Stage 1 category discovery (wiki category pages are hand-curated)
- Populating Achievement IDs (deferred to Phase 2)
- Populating quest descriptions (not used by the plugin)
- Using JournalGenre for categorization (kept as potential future enhancement)
