# FFXIV Todo Plugin — Design Spec

A Dalamud plugin to track unlocking and completion of all non-MSQ content in Final Fantasy XIV.

## Scope

**Tracked content categories:**

- Side Quests (yellow quest markers)
- Feature Unlock Quests (blue plus quest markers)
- Job Quests
- Role Quests
- Trial Series
- Raid Series (8-man normal/savage)
- Alliance Raids
- Beast Tribes
- Custom Deliveries

**Out of scope (v1):** Hunting logs, sightseeing logs, fishing logs, FATEs, treasure maps, deep dungeons.

## Data Model

### Content Database (`content.json` — bundled with plugin)

Structured as `Expansion[] → Category[] → ContentItem[]`.

**ContentItem fields:**

| Field | Type | Description |
|-------|------|-------------|
| `Id` | uint | Unique numeric identifier |
| `Name` | string | Display name |
| `Level` | uint | Required level |
| `Expansion` | enum | ARR, HW, SB, ShB, EW, DT |
| `Category` | enum | SideQuest, BlueUnlock, JobQuest, RoleQuest, TrialSeries, RaidSeries, AllianceRaid, BeastTribe, CustomDelivery |
| `PrerequisiteIds` | uint[] | IDs that must be completed before this is available |
| `LocationTerritoryId` | uint? | Territory for map flagging (nullable) |
| `LocationMapX` | float? | Map X coordinate (nullable) |
| `LocationMapY` | float? | Map Y coordinate (nullable) |
| `QuestId` | uint? | Journal quest ID for auto-detection (nullable) |
| `AchievementId` | uint? | Achievement ID for completion detection (nullable) |
| `WikiUrl` | string? | Link to wiki page for the item (nullable) |

**Example entry:**

```json
{
  "version": 1,
  "items": [
    {
      "id": 1001,
      "name": "The Binding Coil of Bahamut - Turn 1",
      "level": 50,
      "expansion": "ARR",
      "category": "RaidSeries",
      "prerequisiteIds": [1000],
      "locationTerritoryId": 1043,
      "locationMapX": 15.5,
      "locationMapY": 22.3,
      "questId": null,
      "achievementId": 690,
      "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/The_Binding_Coil_of_Bahamut_-_Turn_1"
    }
  ]
}
```

### Progress Store (`progress.json` — local player state)

Flat map of `ContentItem.Id → ProgressEntry`:

```json
{
  "1000": { "status": "Completed", "isTracked": false, "isIgnored": false, "isManual": false },
  "1001": { "status": "InProgress", "isTracked": true, "isIgnored": false, "isManual": false },
  "1002": { "status": "NotStarted", "isTracked": false, "isIgnored": true, "isManual": true }
}
```

**ProgressEntry fields:**

| Field | Type | Description |
|-------|------|-------------|
| `Status` | enum | NotStarted, InProgress, Completed |
| `IsTracked` | bool | Promoted to overlay visibility |
| `IsIgnored` | bool | Hidden from all views (unless "show ignored" is on) |
| `IsManual` | bool | If true, auto-scanner skips this entry |

### Derived Display States (computed at runtime)

- **Locked**: any prerequisite has status != Completed
- **Available**: all prerequisites completed, status = NotStarted
- **InProgress/Completed**: direct from ProgressStore
- **Hidden**: IsIgnored = true

## Architecture

Approach B: Plugin + data layer separation.

```
┌─────────────────────────────────────────────────┐
│  Plugin.cs (entry point, lifecycle)              │
├─────────────────────────────────────────────────┤
│                                                   │
│  ┌──────────────┐  ┌──────────────┐              │
│  │ MainWindow   │  │ OverlayWindow│              │
│  │ (tree view,  │  │ (tracked     │              │
│  │  filters)    │  │  items only) │              │
│  └──────┬───────┘  └──────┬───────┘              │
│         │                 │                      │
│         └────────┬────────┘                      │
│                  ▼                               │
│  ┌───────────────────────────┐                   │
│  │    ContentManager          │                  │
│  │  - Loads content.json     │                  │
│  │  - Resolves prerequisites  │                  │
│  │  - Computes display state  │                  │
│  └───────────────────────────┘                   │
│                  │                               │
│  ┌───────────────┴───────────┐                   │
│  │                           │                   │
│  ▼                           ▼                   │
│  ┌──────────────┐  ┌──────────────────┐         │
│  │ProgressStore │  │  ProgressScanner │         │
│  │(load/save    │  │  (polls journal, │         │
│  │ state JSON)  │  │   achievements)  │         │
│  └──────────────┘  └──────────────────┘         │
│                                                   │
│  ┌──────────────┐                                │
│  │ MapFlagHelper│  (places/removes map flags)    │
│  └──────────────┘                                │
└─────────────────────────────────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|---------------|
| `Plugin` | Dalamud lifecycle, command registration, window toggling |
| `ContentManager` | Loads `content.json`, resolves prerequisite chains, computes per-item display state by joining content data with player progress |
| `ProgressStore` | Loads/saves player progress to `progress.json` in plugin config directory; exposes get/set by item ID |
| `ProgressScanner` | On login and zone change, reads journal and achievements via Dalamud's `QuestManager` and `AchievementManager` APIs; updates `ProgressStore` for non-manual entries |
| `MainWindow` | ImGui tree view: Expansion > Category > Items. Filters, per-item actions, right panel detail view |
| `OverlayWindow` | ImGui overlay showing only tracked items with minimal chrome |
| `MapFlagHelper` | Places map flags using Dalamud's `IMapLinkPayload` and `GameGui.OpenMapWithMapLink()` |

### Data Flow

1. `Plugin` initializes `ContentManager` and `ProgressStore` on startup
2. `ProgressScanner` fires on login/zone change → updates `ProgressStore` → `ContentManager` re-computes display states
3. Both windows read from `ContentManager` (which joins DB + progress)
4. User actions (track, ignore, flag) write through `ContentManager` → `ProgressStore`
5. Map flagging goes through `MapFlagHelper` using territory + coordinates from content data

## UI Design

### Main Window (`/todo` toggles)

A single ImGui window with a left panel (tree) and a right panel (details).

**Left panel — Tree View:**

```
▼ A Realm Reborn (50)
  ▼ Side Quests (12)
    [ ] The Greatest Story Never Told  Lv.30
    [✓] Buried Truth                  Lv.35
    [!] Hungry Like the Wolf          Lv.45  (locked)
  ▶ Blue Unlock Quests (8)
  ▶ Raid Series (3)
▼ Heavensward (42)
  ...
```

Status icons: `[ ]` available, `[✓]` completed, `[~]` started/in progress, `[!]` locked (grayed out).

Tree nodes show item counts. Click to select → populates right panel. Right-click context menu: Track/Untrack, Ignore/Unignore, Flag on Map, Mark as Complete/Not Started.

**Top bar:** filter dropdowns (expansion, category, status) + search text input.

**Right panel — Item Details (shown when item selected):**
- Name, level, expansion, category
- Status display with manual override buttons
- Prerequisites section: lists prerequisites by name with their current status
- "Track this" / "Untrack" button
- "Ignore this" / "Unignore" button
- "Flag on Map" button (grayed out if no location data)
- "Open Wiki" button (opens URL in default browser)
- "Set as Complete" / "Set as Not Started" buttons

**Menu bar:** "Show ignored" toggle + "Refresh" button (re-scan journal) + "About" with data version info.

### Overlay Window (`/todo overlay` toggles)

Minimal ImGui overlay showing only tracked items:

```
 ┌─ Todo ──────────────────────────┐
 │ [~] Binding Coil T1     ARR Lv50 │
 │ [ ] The Howling Eye     ARR Lv50 │
 │ [ ] Eureka Anemos        SB Lv70 │
 │ [!] Bozja Southern     ShB Lv71 │
 └──────────────────────────────────┘
```

- Shows only `IsTracked=true` items, sorted by expansion then level
- Each item shows: status icon, name, expansion badge, level
- Hover: tooltip with full details (category, prereq status)
- Right-click context menu: Flag on Map, Untrack, Mark Complete

**Configurable settings (saved to plugin config):**
- Overlay position (X, Y) and width
- Background opacity (0–100%)
- Max visible items before scrolling
- Always-on-top toggle
- Font scale

## Progress Scanning

**Triggers:**
1. On login (full scan)
2. On zone change (debounced incremental scan)

**Quest detection** (via Dalamud's `QuestManager`):
- `QuestManager.IsQuestComplete(questId)` for completed quests
- `QuestManager.GetQuestList()` for accepted/in-progress quests

**Achievement detection** (via Dalamud's `AchievementManager`):
- `AchievementManager.IsComplete(achievementId)` for duty/system completions

**Manual override protection:**
- Scanner skips any entry where `IsManual = true`
- When user manually sets status via UI, `IsManual` is set to `true`
- A "Reset to Auto" button clears the manual flag
- Manual overrides are visually distinct in the UI (italicized with a `*`)

**Edge cases:**
- QuestId and AchievementId both null: item is purely manual
- Both QuestId and AchievementId present: completion = whichever completes first (union, not intersection)

## Map Flagging

**Flow when user clicks "Flag on Map":**
1. Read `LocationTerritoryId`, `LocationMapX`, `LocationMapY` from content item
2. Create map link payload for that territory and coordinates
3. Call `GameGui.OpenMapWithMapLink()` to open and center the in-game map
4. Place a map marker at the coordinates via Dalamud's map marker API

**Constraints:**
- Only one flag active at a time (FFXIV limitation)
- If `LocationTerritoryId` is null/0, the button is grayed out with tooltip "No map location available"
- v1 stores only the start/quest-giver location

## Commands

| Command | Action |
|---------|--------|
| `/todo` | Toggle main window |
| `/todo overlay` | Toggle tracked overlay |
| `/todo refresh` | Force re-scan journal/achievements |

## Configuration

Plugin config saved via Dalamud's config system:
- Overlay position, size, opacity, font scale
- "Show ignored" default state
- Scan debounce interval (default: 2 seconds on zone change)

## Out of Scope (v2+)

- Additional content categories (FATEs, deep dungeons, hunting logs, etc.)
- Live online data fetching
- Filter presets / saved views
- Multi-character profile support
- Custom content item creation
