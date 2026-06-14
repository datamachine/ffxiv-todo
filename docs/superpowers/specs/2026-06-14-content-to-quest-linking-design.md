# Content-to-Quest Linking Spec

Link content items (raid series, trial series, alliance raids) to their unlock quest chains so users can see which quests are needed to unlock each content item.

## Goals

- Content items (e.g. "Eden's Gate") remain the primary display name in the list.
- Each content item can point to an ordered chain of unlock quests via `UnlockQuestIds: uint[]`.
- Unlock quests exist as separate `ContentItem` entries, grouped visually under their parent content.
- Completing the last quest in a chain auto-completes the parent content item.
- Data population uses a hybrid approach: wiki scraping as the primary source, with a manual override file for edge cases.

## Non-Goals

- No changes to `ProgressEntry` schema.
- No bidirectional auto-completion (content completion does not auto-complete quests).
- No changes to how achievements are tracked.

## Data Model

### ContentItem

Add one field:

```csharp
public uint[] UnlockQuestIds { get; set; } = [];
```

An ordered list of quest IDs that must be completed to unlock this content. The order represents the chain: quest `[0]` must be completed before `[1]`, etc. Empty array means no associated quest chain.

Each quest in the chain is also a `ContentItem` entry (category `BlueUnlock` or similar), with its own `QuestId`, location data, level, and prerequisites. The link is unidirectional: content → quests.

### content.json

Relevant entries gain an `unlockQuestIds` field:

```json
{
  "id": 427,
  "name": "Eden's Gate",
  "level": 80,
  "expansion": "ShB",
  "category": "RaidSeries",
  "unlockQuestIds": [69163],
  "questId": null,
  "achievementId": 2409,
  "wikiUrl": "https://ffxiv.consolegameswiki.com/wiki/Eden%27s_Gate"
}
```

### Quest Chain Override File

Checked into the repo at `DataBuilder/Data/quest_chain_overrides.json`:

```json
[
  { "contentId": 427, "questIds": [69163] },
  { "contentId": 428, "questIds": [69163, 69262, 69357] }
]
```

Entries in this file take absolute priority. If a content ID is present here, wiki scraping is skipped for that item.

## Data Builder Pipeline

### New Stage: UnlockQuestResolver

Position: after `CsvEnricher`, before `WikiDetailScraper`.

**Step 1 — Load overrides:**
Read `quest_chain_overrides.json`. For each entry, validate that all `questIds` exist in the Quest CSV. Populate `UnlockQuestIds` on the matching `DetailItem`. Mark these items as resolved; skip wiki scraping for them.

**Step 2 — Wiki scrape (fallback):**
For each `RaidSeries`, `TrialSeries`, and `AllianceRaid` content item without an override and without a populated `QuestId`:

1. Scrape the item's wiki page.
2. Parse the "Unlocks" or quest prerequisite section (e.g., "The raid can be unlocked by completing the quest In the Middle of Nowhere").
3. Extract quest names from the parsed text.
4. Match quest names against the Quest CSV to get quest IDs.

**Step 3 — Build quest chains:**
For each quest ID found:

1. Look up the quest in the Quest CSV.
2. Walk backward through `PreviousQuest0`, `PreviousQuest1`, `PreviousQuest2` to discover the full prerequisite chain.
3. Collect all quest IDs in dependency order (first quest → last quest).
4. Add any quest not already in the content list as a new `ContentItem` (category `BlueUnlock`).
5. Set `UnlockQuestIds` on the `DetailItem` to the ordered chain.

**Step 4 — Write output:**
`FormattedItem` includes `UnlockQuestIds`. These are serialized into `content.json`.

### Quest Chain Entry Creation

When a quest from the chain is not already in the content list, create a `ContentItem` for it:

- `Name`: quest name from Quest CSV
- `Level`: `ClassJobLevel` from Quest CSV
- `Expansion`: mapped from Quest CSV expansion value
- `Category`: `BlueUnlock`
- `QuestId`: quest ID from Quest CSV
- `LocationTerritoryId`/`Name`: resolved from Quest CSV `IssuerLocation` and `PlaceName` fields
- `LocationMapX`/`LocationMapY`: left null (Quest CSV has no coordinate data; ENPC position resolution is out of scope)
- `PrerequisiteIds`: mapped from `PreviousQuest0-2` in Quest CSV
- `UnlockQuestIds`: empty (the quest itself IS the unlock)

## UX

### Content Row Behavior

When a content item has `UnlockQuestIds` with incomplete quests:

- The row shows the name of the **next incomplete quest** in the chain beneath the content name, e.g.:
  ```
  Eden's Gate
    → In the Middle of Nowhere
  ```
- An expand toggle (arrow/chevron) reveals the quest chain group.

When all quests in the chain are completed, no quest indicator is shown.

### Quest Chain Group

Rendered as indented rows under the parent content item:

- Each quest shows its name, level, location, and completion state.
- Only the first incomplete quest is actionable (clickable, can mark complete). Subsequent locked quests are grayed.
- Completed quests show as checked but remain visible.
- The expand/collapse state is per-content-item and not persisted.

### Auto-Completion

When the user marks the last quest in a chain as complete, the parent content item is automatically marked complete. In `ProgressStore.SetStatus`, after updating the quest's progress, if the quest is the last entry in any parent content's `UnlockQuestIds` and the new status is `Completed`, the parent is set to `Completed` as well.

Auto-completion propagates only quest → content, never reverse.

### Filtering

- The quest chain group respects active filters. If the parent content is hidden by a filter, the quest group is hidden too.
- Individual quests in the chain inherit the parent content's category for filtering purposes (a quest in Eden's Gate's chain filters as `RaidSeries`).
- Implementation: at render time, look up the parent content item for each chain quest via a reverse index built from `UnlockQuestIds`.

## Progress Tracking

### ProgressStore Changes

Extend `SetStatus` to detect chain completion:

1. Update the item's own progress to the given status.
2. If the new status is `Completed`, check if this item appears as the last entry in any other content item's `UnlockQuestIds`.
3. If so, call `SetStatus` on the parent content item with `Completed`.

No new storage fields. The `UnlockQuestIds` relationship is read from `content.json` at runtime.

### Edge Cases

- **Partial chain**: User completes quest 1 of 3, quits. On next login, quest 2 is the actionable item. Progress on quest 1 persists.
- **Solo quest**: A content item with exactly 1 unlock quest behaves identically. Complete the quest → content auto-completes.
- **Quest rework by SE**: If a patch changes the quest chain, update the override file and rebuild `content.json`. Stale progress on old quests can be cleared via manual tracking.
- **Manual override**: The existing `IsManual` flag on `ProgressEntry` allows the user to manually mark content complete even if the chain is incomplete. Auto-completion does not overwrite manual entries.
- **Quest appearing in multiple chains**: A quest that unlocks two different content items (rare but possible) would trigger auto-completion of both when completed.
- **Chain with no content parent**: If a quest has a `QuestId` but no content item links to it, it behaves as a standalone `BlueUnlock` with no auto-completion side effects.

## Implementation

### Files Changed

| File | Change |
|------|--------|
| `FfxivTodo/Models/ContentItem.cs` | Add `UnlockQuestIds` field |
| `FfxivTodo/Models/PipelineModels.cs` | Add `UnlockQuestIds` to `DetailItem` and `FormattedItem` |
| `FfxivTodo/Services/ProgressStore.cs` | Extend `SetStatus` with chain-aware auto-completion |</｜DSML｜parameter>, <｜DSML｜parameter name=
| `FfxivTodo/Windows/MainWindow.cs` | Add quest chain group rendering, expand toggle, next-quest indicator |
| `DataBuilder/Data/quest_chain_overrides.json` | New manual override file |
| `DataBuilder/Scrapers/UnlockQuestResolver.cs` | New pipeline stage |
| `DataBuilder/Program.cs` | Wire new stage into pipeline |

### UnlockQuestResolver Implementation

The scraper uses AngleSharp (already a dependency) to parse wiki pages. Quest name extraction uses regex patterns matched against the wiki page's "Unlocks" or "Quest" section. The patterns are:

- `completing the quest (.+?)(?:\.|,)` 
- `starting the quest (.+?)(?:\.|,)`
- `the quest (.+?) must be completed`

Quest chain building uses `QuestCsvRow.PreviousQuest0/1/2` to walk backward through prerequisites. The chain is reversed to produce dependency order.

## Testing

### DataBuilder Tests

- `UnlockQuestResolver` resolves a raid series from override file
- `UnlockQuestResolver` scrapes wiki page and extracts quest names
- `UnlockQuestResolver` builds a multi-step chain from `PreviousQuest0-2`
- `UnlockQuestResolver` skips wiki scraping when override exists
- Quest chain entries are created with correct category and expansion

### Plugin Tests

- `ProgressStore.MarkCompleted` auto-completes parent when last quest in chain
- `ProgressStore.MarkCompleted` does not auto-complete when quest is mid-chain
- `ProgressStore.MarkCompleted` does not auto-complete when quest is not in any chain

### Test Fixtures

Add wiki HTML pages for raid series with unlock quest sections (e.g., `eden_gate.html`, `nier_raid.html`). Add override JSON test files.

## Risks

- Wiki page structure varies across content items; regex parsing may miss some quest names. Mitigation: the override file catches failures; missing chains are non-fatal (content renders as today).
- Quest CSV prerequisite fields may have gaps or refer to quests not in the CSV (limited CSV scope). Mitigation: skip unresolvable prerequisites rather than failing.
- Expanding tree groups may impact scroll performance with many items. Mitigation: lazy-render group contents only when expanded.
