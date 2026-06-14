# Filter Redesign Spec

Improve the main window filter bar so it supports multi-select filtering, friendly labels, and explicit filtering of blocked and ignored items.

## Goals

- Replace awkward single-select enum dropdowns with clearer controls.
- Allow selecting multiple expansions and multiple categories at once.
- Make status filtering explicit for `Not started`, `In progress`, `Completed`, `Locked`, and `Ignored`.
- Preserve the existing content data model and progress storage format.

## Non-Goals

- No changes to `content.json` schema.
- No changes to `progress.json` schema.
- No redesign of the tree view or detail panel beyond filter integration.

## UX

### Top Row

The top filter row contains:

- `Expansion` label + popup multi-select button
- `Category` label + popup multi-select button
- `States` label + always-visible toggle chips
- Search box
- `Clear filters` button

### Expansion Filter

Expansion uses a popup multi-select checklist.

Displayed labels:

- `A Realm Reborn`
- `Heavensward`
- `Stormblood`
- `Shadowbringers`
- `Endwalker`
- `Dawntrail`

The button summary rules:

- No selections: `All`
- One or two selections: comma-separated labels
- Three or more selections: `<n> selected`

### Category Filter

Category uses a popup multi-select checklist.

Displayed labels:

- `Side quests`
- `Unlock quests`
- `Job quests`
- `Role quests`
- `Trial series`
- `Raid series`
- `Alliance raids`
- `Tribal quests`
- `Custom deliveries`

`BlueUnlock` is presented as `Unlock quests`.
`BeastTribe` is presented as `Tribal quests`.

The summary rules match the expansion filter.

### State Filter

State filtering is exposed as always-visible toggle chips:

- `Not started`
- `In progress`
- `Completed`
- `Locked`
- `Ignored`

Each chip is independently toggleable.
If no chips are active, state filtering is treated as `All`.

### Search

Search remains a case-insensitive substring match on item name.

### Clear Filters

`Clear filters` resets:

- expansion selections
- category selections
- state selections
- search text

## Filter Semantics

### Group Logic

- Inside each filter group, matching is `OR`.
- Across filter groups, matching is `AND`.

Examples:

- Expansions `EW` and `DT` means items from either expansion.
- States `Completed` and `Locked` means items in either state.
- Category `Unlock quests` plus state `Locked` means only locked unlock quests.

### Empty Selection Semantics

- No expansions selected means all expansions.
- No categories selected means all categories.
- No states selected means all display states.

### Derived Display State

The UI uses a derived filter state rather than raw stored status.

Precedence:

1. `Ignored`
2. `Locked`
3. Stored progress status (`Not started`, `In progress`, `Completed`)

This allows `Locked` to be filtered separately from ordinary `Not started`.

## Implementation

The implementation stays local to `FfxivTodo/Windows/MainWindow.cs` unless a helper becomes necessary for readability.

### State Model

Replace the current nullable enum filters:

- `Expansion? _filterExpansion`
- `ContentCategory? _filterCategory`
- `ItemStatus? _filterStatus`

with selection sets:

- `HashSet<Expansion> _selectedExpansions`
- `HashSet<ContentCategory> _selectedCategories`
- `HashSet<FilterState> _selectedStates`

Add a UI-facing enum:

```csharp
private enum FilterState
{
    NotStarted,
    InProgress,
    Completed,
    Locked,
    Ignored
}
```

### Label Helpers

Add local helpers for:

- expansion display labels
- category display labels
- state chip labels
- popup summary text

These helpers affect only rendering, not persistence.

### Filtering

Update tree generation and `FilterItems` so that:

- expansion/category filters check membership in the corresponding selection set when non-empty
- state filtering uses derived display state precedence
- ignored items are no longer controlled by a separate menu toggle

The existing menu bar `Show Ignored` toggle should be removed because `Ignored` becomes a first-class filter state.

## Testing

Add tests around the new filtering behavior and label mapping.

Coverage should include:

- empty selection means all
- multi-select expansion/category OR behavior
- multi-select state OR behavior
- `Ignored` precedence over `Locked` and raw progress status
- `Locked` items do not match `Not started`
- friendly summary text for one, two, and many selections
- clear filters resets all filter inputs

## Risks

- The top row may become cramped in narrow widths.
- ImGui popup state can become messy if helper code is too implicit.

Mitigation:

- keep summaries short
- keep state chips concise
- keep filter helper methods local and explicit
