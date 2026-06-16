# FFXIV Todo

A Dalamud plugin that tracks non-MSQ content completion across every expansion. Auto-detects progress from your quest journal and achievements, and shows what content you've unlocked, what's in progress, and what you haven't started yet.

## Features

- **Tree view** — all non-MSQ content organized by expansion and category: job quests, role quests, raid series, alliance raids, savage/ultimate/chaotic raids, field operations, variant dungeons, deep dungeons, relic weapons, optional dungeons, PvP, allied society quests, custom deliveries, and more
- **Auto-detection** — checks your quest journal and achievement list to mark items as Not Started / In Progress / Unlocked / Completed
- **Quest chain tracking** — content items with unlock quest chains auto-complete via `AutoCompleteParents`
- **Achievement-based detection** — items with achievements use them as the definitive completion signal. Detail panel shows the achievement name at runtime
- **Detail panel** — click any item to see its unlock quests (grouped by status with colored headers), achievement info with wiki link, prerequisites, and location
- **Flag on map** — places a map flag on the first Not Started quest in a chain, or the content item itself
- **Overlay** — compact always-on-top window (`/todotracker`) showing up to 10 tracked items; hidden when not logged in
- **Filtering** — filter by expansion, category, and completion status (Not Started / In Progress / Unlocked / Completed / Locked / Ignored)
- **Search** — text search across all content names
- **Manual override** — manually set any item's status

## Commands

| Command | Action |
|---|---|
| `/todo` | Toggle main window |
| `/todo refresh` | Re-scan all items |
| `/todotracker` | Toggle overlay window |

## Content tracked

Across all 6 expansions (ARR through Dawntrail), the plugin tracks 500+ content items across 21 categories:

Job quests, role quests, raid series, alliance raids, savage raids, ultimate raids, chaotic raids, field operations (Eureka/Bozja/Occult), variant & criterion dungeons, deep dungeons, relic weapons, optional dungeons, PvP modes, allied society quests, custom deliveries, side quests, island sanctuary, ishgardian restoration, faux hollows, the masked carnivale, the gold saucer, treasure hunts, and your companion chocobo.

## Installation

1. Install [Dalamud](https://github.com/goatcorp/Dalamud) via [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher)
2. In-game, open the Dalamud Plugin Installer (`/xlplugins`)
3. Open **Settings** → **Experimental** → **Custom Plugin Repositories**
4. Add `https://datamachine.net/ffxiv-plugins/pluginmaster.json`
5. Save, then search for **FFXIV Todo** and click **Install**

Updates are automatic — Dalamud checks the repo URL for new versions.

### From source

```
dotnet build
```

Copy the output to your Dalamud `devPlugins` folder, or use Dalamud's dev plugin loading to point at the build output.

## How it works

### Data pipeline (`DataBuilder`)

The `DataBuilder` console app generates the data file that the plugin loads at startup:

```
wiki pages ──► scraper ──► CSV enrichment ──► quest chain resolution ──► content.json
```

1. **Scrape** — parses wiki pages (Job Quests, Raids, Feature Quests, Role Quests, etc.) for item names, categories, and expansions
2. **Enrich** — matches scraped items against CSV data (quest IDs, achievement IDs, levels, locations)
3. **Resolve** — walks prerequisite chains, creates BlueUnlock entries for quest chain tracking
4. **Format** — writes `FfxivTodo/Data/content.json` (embedded as a resource in the plugin)

Run from the repo root:

```
dotnet run --project DataBuilder/DataBuilder.csproj
```

Use `--from categories` to skip re-scraping and re-run enrichment + chain resolution from cached wiki data.

### Runtime (`FfxivTodo`)

At startup the plugin loads `content.json` from its embedded resources. On login and zone changes it scans your quest journal and achievements, updating each item's status. Manual overrides are persisted to a `progress.json` file in the plugin config directory.

## Developer commands

```
dotnet build                    # Build everything
dotnet test DataBuilder.Tests   # Run DataBuilder unit tests
dotnet test FfxivTodo.Tests     # Run plugin unit tests
dotnet run --project DataBuilder -- --output FfxivTodo/Data/content.json  # Regenerate data
```

### Override files

- `DataBuilder/Data/quest_chain_overrides.json` — map content items to quest ID chains. Use `"explicitChain": true` to avoid walking into MSQ territory
- `DataBuilder/Data/achievement_overrides.json` — link content items to achievement IDs when name matching isn't possible

### Adding new content categories

Each new category needs updates in 3 places plus the scraper:

1. `FfxivTodo/Models/Enums.cs` — add enum value at the end
2. `DataBuilder/Formatters/ContentJsonFormatter.cs` — add `CategoryOrder` entry
3. `FfxivTodo/Windows/MainWindowFilterLogic.cs` — add `GetCategoryLabel` entry
4. `DataBuilder/Scrapers/WikiCategoryScraper.cs` — add parser or static items, wire into `ScrapeAllAsync()`

## Project structure

```
DataBuilder/          Console app for data generation
  Data/               Override files (quest chains, achievements)
  Scrapers/           Wiki parsers, CSV enrichment, chain resolution
  Formatters/         Output formatting to content.json
  Tests/              xUnit tests with HTML fixtures
FfxivTodo/            Dalamud plugin
  Data/content.json   Generated data file (embedded resource)
  Services/           Content manager, progress store, scanner, map flags
  Windows/            ImGui UI (main window + overlay + filter logic)
  Models/             Enum definitions
```

## License

MIT
