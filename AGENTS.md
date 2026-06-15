# AGENTS.md

## Project structure

Two projects in one solution:
- `DataBuilder/` — .NET 10 console app that scrapes wiki + CSV data and generates `FfxivTodo/Data/content.json`
- `FfxivTodo/` — .NET 10 Dalamud plugin (net10.0-windows, Dalamud.NET.Sdk 15.0) that loads content.json and tracks completion in-game

## Key directories

| Path | Purpose |
|---|---|
| `DataBuilder/Scrapers/` | Wiki parsers + CSV enrichment + quest chain resolution |
| `DataBuilder/Formatters/` | Output formatting to content.json |
| `DataBuilder/Data/` | Override files (quest chains, achievements) |
| `Cache/` | Intermediate pipeline output + CSV downloads |
| `FfxivTodo/Data/content.json` | Generated output — the app's data file |
| `FfxivTodo/Services/` | Runtime logic (scanning, progress, map flags) |
| `FfxivTodo/Windows/` | ImGui UI |
| `FfxivTodo/Models/` | Enum definitions shared by both projects |

## Data generation pipeline

Run from repo root — fetches wiki pages + CSVs, processes, outputs content.json:

```
dotnet run --project DataBuilder/DataBuilder.csproj
```

Pipeline stages (can be re-run from cache with `--from`):
1. **Wiki scrape** → `Cache/category_items.json` (CategoryItem: name, category, expansion)
2. **CSV enrichment** → `Cache/detail_items.json` (DetailItem: +questId, achievementId, level, location, prerequisites)
3. **Quest chain resolution** → adds `UnlockQuestIds`, creates BlueUnlock items for unresolved chain quests
4. **Wiki detail scrape** → merges location/level data from wiki pages
5. **Format** → writes `FfxivTodo/Data/content.json`

Use `--from categories` to re-run enrichment + chain resolution without re-scraping.

## Key data files

- `DataBuilder/Data/quest_chain_overrides.json` — maps content item names to quest ID chains. `explicitChain: true` = use the exact quest list; `false` = walk prerequisite chain backward from terminal quest
- `DataBuilder/Data/achievement_overrides.json` — maps content names to achievement IDs when name-based matching fails
- `FfxivTodo/Data/content.json` — embedded resource loaded by the plugin at startup

## Data conventions

- **BlueUnlock** items are hidden from tree view (exist only for quest chain walking + detail panel display)
- **Category enum** exists in two places: `DataBuilder/Models/PipelineModels.cs` (string, no enum) and `FfxivTodo/Models/Enums.cs` (C# enum). Adding categories requires updating BOTH the enum AND `CategoryOrder` in `ContentJsonFormatter.cs` AND `GetCategoryLabel` in `MainWindowFilterLogic.cs`
- **Expansions** use short codes: ARR, HW, SB, ShB, EW, DT
- **BeastTribe** items are renamed from location names to society names via `BeastTribeNames` dictionary in formatter
- **NormalizeName** strips Unicode PUA characters (U+E000–U+F8FF) from quest names at CSV load time
- **NameCategoryMap** assigns categories to items with empty category by name pattern matching (used for Savage/Ultimate/FieldOperation/VariantDungeon/ChaoticRaid items scraped without category from the Raids page)

## Wiki scraping details

- Wiki base: `https://ffxiv.consolegameswiki.com`
- User-Agent header required: `FfxivTodo-DataBuilder/1.0`
- MediaWiki heading IDs get `_2`, `_3` numeric suffixes for duplicate heading text — parsers must strip `_\d+$` when matching heading IDs
- Unlock quests for RaidSeries/TrialSeries/AllianceRaid content items are resolved by scraping each item's wiki URL for "completing the quest X" patterns
- New content types that need categories must be added to the `CategoryOrder` dictionary for proper sorting

## Testing

```
dotnet test DataBuilder.Tests/DataBuilder.Tests.csproj
dotnet test FfxivTodo.Tests/FfxivTodo.Tests.csproj
```

Both use xUnit. DataBuilder tests have HTML test fixtures in `DataBuilder.Tests/TestData/`.

## Build

```
dotnet build                    # builds both projects
dotnet build DataBuilder/DataBuilder.csproj   # DataBuilder only
```

Plugin build uses `Dalamud.NET.Sdk/15.0.0` targeting `net10.0-windows`. Pre-existing warnings in `OverlayWindow.cs:35` (null dereference) and other minor warnings are not new.

## Content tracking (runtime)

The plugin auto-detects completion via two mechanisms:
1. **Quest-based** — `ScanItem` checks quest journal for each item's `QuestId`; `AutoCompleteParents` walks `UnlockQuestIds` chains to auto-complete parent items
2. **Achievement-based** — when achievement list is loaded, achievement status takes priority over quest chains for items that have both a `AchievementId` and `UnlockQuestIds`

## Common pitfalls

- **"Could not reload manifest" error** is a Dalamud dev plugin hot-reload race condition — stop the plugin in Dalamud before rebuilding
- **New ContentCategory enum values** must be added in order at the end; the enum is serialized as strings via `StringEnumConverter`
- **Chain walking** with `explicitChain: false` walks ALL prerequisites (including MSQ) — use `explicitChain: true` with specific quest IDs for chains that branch into MSQ territory
- **DistinctBy** in `ScrapeAllAsync` dedup by item name — two scrapers producing the same name results in only the first one surviving
