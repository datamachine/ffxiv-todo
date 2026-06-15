# Additional Content Types Design

**Date**: 2026-06-14
**Status**: Design

## Overview

Add 7 missing content types to ffxiv-todo, covering ~35 new items with wiki-scraped data, achievement tracking, and quest chain linking.

## Background

The current content.json has 680 items across 12 categories. Seven content types are missing from the wiki scrape — Role Quests, Variant & Criterion Dungeons, Relic Weapons, Island Sanctuary, Faux Hollows, The Masked Carnivale, and Ishgardian Restoration. All have scrapeable wiki pages and existing achievements in the CSV.

## Categories

### New/expanded categories

| Enum value | Label | New? | Item count |
|---|---|---|---|
| `RoleQuest` | "Role quests" | Exists, unused | 17 |
| `VariantDungeon` | "Variant dungeons" | Exists, unused | Expand 1 to 8 (+7) |
| `RelicWeapon` | "Relic weapons" | New | 7 |
| `IslandSanctuary` | "Island Sanctuary" | New | 1 |
| `FauxHollows` | "Faux Hollows" | New | 1 |
| `MaskedCarnivale` | "The Masked Carnivale" | New | 1 |
| `IshgardianRestoration` | "Ishgardian Restoration" | New | 1 |

## Item Specs

### Role Quests (17 items)

Scrape from `/wiki/Role_Quests`. Structured tables with quest names, levels, quest giver.

**Shadowbringers** (5):
- Tank, Physical DPS, Magical Ranged DPS, Healer, Master

**Endwalker** (6):
- Tank, Melee DPS, Physical Ranged DPS, Magical Ranged DPS, Healer, Master

**Dawntrail** (6):
- Tank, Melee DPS, Physical Ranged DPS, Magical Ranged DPS, Healer, Master

Each item links to its role-specific quest chain (6-7 quests per role). Item names follow pattern: `"<Expansion> <Role> Role Quests"` (e.g., "Shadowbringers Tank Role Quests").

### Variant & Criterion Dungeons (8 items)

Scrape from `/wiki/Variant_and_Criterion_Dungeons`. Table with dungeon names and unlock quests.

| Dungeon base | Variant | Criterion |
|---|---|---|
| The Sil'dihn Subterrane | The Sil'dihn Subterrane | Another Sil'dihn Subterrane |
| Mount Rokkon | Mount Rokkon | Another Mount Rokkon |
| Aloalo Island | Aloalo Island | Another Aloalo Island |
| The Merchant's Tale | The Merchant's Tale | Another Merchant's Tale |

Variant entries link to their unlock quests (e.g. "A Key to the Past" for Sil'dihn). Criterion entries require completing the corresponding Variant first, then unlock via a conversation with Osmon.

### Relic Weapons (7 items)

Scrape from Feature Quests page, Side Story → Relic Weapons section (currently skipped). One per series:

| Series | Expansion | Unlock quest |
|---|---|---|
| Zodiac Weapons | ARR | A Relic Reborn |
| Anima Weapons | HW | An Unexpected Proposal |
| Eureka Weapons | SB | And We Shall Call It Eureka |
| Resistance Weapons | ShB | Where Eagles Nest |
| Skysteel Tools | ShB | Towards the Firmament (same as Ishgardian Restoration) |
| Manderville Weapons | EW | Somehow Further Hildibrand Adventures quests |
| Phantom Weapons | DT | One Last Hurrah |

Each item has a single unlock quest defining the chain start. Subsequent relic grinds are not quests and not tracked. These are separate from existing FieldOperation entries (Baldesion Arsenal, Castrum/Delubrum/Dalriada) which track the area/instance content.

### Single-entry content (4 items)

| Item | Wiki page | Unlock quest | Category |
|---|---|---|---|
| Island Sanctuary | `/wiki/Island_Sanctuary` | Seeking Sanctuary | `IslandSanctuary` |
| Faux Hollows | `/wiki/Faux_Hollows` | Fantastic Mr. Faux | `FauxHollows` |
| The Masked Carnivale | `/wiki/The_Masked_Carnivale` | The Real Folk Blues | `MaskedCarnivale` |
| Ishgardian Restoration | `/wiki/Ishgardian_Restoration` | Towards the Firmament | `IshgardianRestoration` |

Each has a quest chain and/or achievements. Island Sanctuary has a 6-quest chain. The others have single unlock quests plus achievements.

## Wiki Scraper Changes

### New parsers in WikiCategoryScraper.cs

1. **`ParseRoleQuestsPage(HtmlNode contentNode)`** — parses `/wiki/Role_Quests`
   - Walks h2 → h3 structure (ShB, EW, DT sections)
   - Under each expansion, h4 headings identify roles
   - Table rows contain quest names in first cell links
   - Outputs items with `Category = "RoleQuest"`, expansion from heading

2. **`ParseVariantDungeonsPage(HtmlNode contentNode)`** — parses `/wiki/Variant_and_Criterion_Dungeons`
   - Two sections: Variant Dungeons and Criterion Dungeons
   - Table rows contain dungeon names
   - Unlock quest references in the unlock column
   - Outputs items with `Category = "VariantDungeon"`

3. **`ParseIslandSanctuaryPage(HtmlNode contentNode)`** — parses `/wiki/Island_Sanctuary`
   - Questline table with 6 rows
   - First cell links are quest names
   - Outputs 1 item with `Category = "IslandSanctuary"`

4. **`ParseFauxHollowsPage(HtmlNode contentNode)`** — parses `/wiki/Faux_Hollows`
   - Unlock quest in Requirements section
   - Achievement table lists "Friend or Faux I-IV"
   - Outputs 1 item with `Category = "FauxHollows"`

5. **`ParseMaskedCarnivalePage(HtmlNode contentNode)`** — parses `/wiki/The_Masked_Carnivale`
   - Unlock quest in Unlock section
   - Achievement table
   - Outputs 1 item with `Category = "MaskedCarnivale"`

6. **`ParseIshgardianRestorationPage(HtmlNode contentNode)`** — parses `/wiki/Ishgardian_Restoration`
   - Main quest chain + side quests section
   - 11 Skybuilder achievements
   - Outputs 1 item with `Category = "IshgardianRestoration"`

### Modifications to ParseFeatureQuestsPage

Un-skip the following sections (currently `currentCategory = string.Empty`):

- `Records_of_Unusual_Endeavors` — set `currentCategory = "BlueUnlock"` so variant dungeon quests, Ishgardian Restoration main quests, etc. are collected
- `Relic_Weapons` — set `currentCategory = "BlueUnlock"` so relic unlock quests are collected

These BlueUnlock quests serve as the quest chain data that the new content items link to.

### Wire into ScrapeAllAsync

Add new fetches:
```csharp
// Role Quests
var roleItems = await FetchAndParseAsync("/wiki/Role_Quests", doc =>
    ParseRoleQuestsPage(doc.DocumentNode));
allItems.AddRange(roleItems);

// Variant & Criterion Dungeons
var variantItems = await FetchAndParseAsync("/wiki/Variant_and_Criterion_Dungeons", doc =>
    ParseVariantDungeonsPage(doc.DocumentNode));
allItems.AddRange(variantItems);

// Island Sanctuary
var islandItems = await FetchAndParseAsync("/wiki/Island_Sanctuary", doc =>
    ParseIslandSanctuaryPage(doc.DocumentNode));
allItems.AddRange(islandItems);

// Faux Hollows
var fauxItems = await FetchAndParseAsync("/wiki/Faux_Hollows", doc =>
    ParseFauxHollowsPage(doc.DocumentNode));
allItems.AddRange(fauxItems);

// The Masked Carnivale
var carnivaleItems = await FetchAndParseAsync("/wiki/The_Masked_Carnivale", doc =>
    ParseMaskedCarnivalePage(doc.DocumentNode));
allItems.AddRange(carnivaleItems);

// Ishgardian Restoration
var restoItems = await FetchAndParseAsync("/wiki/Ishgardian_Restoration", doc =>
    ParseIshgardianRestorationPage(doc.DocumentNode));
allItems.AddRange(restoItems);
```

## Formatter Changes

### ContentJsonFormatter.cs

1. Add `CategoryOrder` entries:
```csharp
["RoleQuest"] = 3,
["RelicWeapon"] = 15,
["IslandSanctuary"] = 16,
["IshgardianRestoration"] = 17,
["FauxHollows"] = 18,
["MaskedCarnivale"] = 19,
```

2. No `NameCategoryMap` changes needed — categories are assigned by scrapers

3. No `BeastTribeNames` changes needed

### MainWindowFilterLogic.cs

Add `GetCategoryLabel` entries:
```csharp
ContentCategory.RelicWeapon => "Relic weapons",
ContentCategory.IslandSanctuary => "Island Sanctuary",
ContentCategory.IshgardianRestoration => "Ishgardian Restoration",
ContentCategory.FauxHollows => "Faux Hollows",
ContentCategory.MaskedCarnivale => "The Masked Carnivale",
```

### ContentCategory.cs

Add new enum values:
```csharp
RelicWeapon,
IslandSanctuary,
IshgardianRestoration,
FauxHollows,
MaskedCarnivale,
```

## Quest Chain Linking

### Strategy

All new content items link to BlueUnlock quests as their unlock chains. After the scraper changes above, most of these BlueUnlock quests will exist in the data. For quest chains that need `explicitChain` overrides (mostly the multi-quest role quest chains), entries are added to `quest_chain_overrides.json`.

### AutoCompletion

AutoCompleteParents works as-is. The content items will auto-complete when all quests in their chain are done. For items with achievement-based detection (Faux Hollows, Masked Carnivale, Ishgardian Restoration), the achievements provide additional completion signals through the existing achievement override system.

## Expected Impact

| Metric | Before | After |
|---|---|---|
| Total items in content.json | 680 | ~715 |
| Categories with items | 12 | 17 (5 existing were unused) |
| New scrapers | 0 | 6 |
| Modified scrapers | 0 | 1 |
| New enum values | 0 | 5 |

## Edge Cases

- **Role quest names**: The `/wiki/Role_Quests` page uses heading IDs like `Tank_2` that need deduplication stripping (existing `_\d+$` regex already handles this in ParseRaidsPage)
- **Relic weapons vs Ishgardian Restoration**: Both share "Towards the Firmament" as an unlock quest. The BlueUnlock quest belongs to one content item's chain; the other links to it too
- **Variant Dungeon unlock**: "An Odd Job" is the meta-unlock for all variant dungeons. Each individual dungeon then has its own quest. The chain walker must handle this two-level structure
- **Masked Carnivale stages**: 32 stages exist but only the unlock quest matters for completion tracking. Stage-clearing achievements provide cumulative progress
- **Faux Hollows weekly model**: The unlock quest is a one-time completion. Achievements track cumulative plays (1, 5, 20, 50). AutoCompleteParents can use the highest-tier achievement as the completion signal
