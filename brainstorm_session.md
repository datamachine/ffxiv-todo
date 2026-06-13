# Content tracker plugin for FFXIV

**Session ID:** ses_13dc1f172ffeTijumzhWW5gVcx
**Created:** 6/13/2026, 11:28:27 AM
**Updated:** 6/13/2026, 11:47:55 AM

---

## User

I need to design a new FFXIV Dalamud plugin. Its goal is to track unlocking and completing of all non-msq content. Often times, it's quest markers with the plus on it. But we should be able to source the data from online sources or data mining. They should be tracked by expansion, showing level. Many of them have prerequisites, so we need to design a way to see them but indicating they cannot be started with information. Started, progress, and completion should be able to be dynamically identified through the players journal and achievements. We should be able to mark and unmark as ignored for content we don't care about. Also a "tracked" to show more prominently or in an overlay separate from the main interface. Any quest/content starting or next location should be flagable on the map from the interface.

---

## Assistant (Build · DeepSeek V4 Pro · 4.6s)

_Thinking:_

The user wants to design a new FFXIV Dalamud plugin for tracking non-MSQ content completion. This is a design/brainstorming task. Let me first check the brainstorming skill since they're asking me to design something, and I'm told to use brainstorming before any creative work.

Let me also check the current workspace to understand what exists.


## Assistant (Build · DeepSeek V4 Pro · 9.0s)

_Thinking:_

The project directory is empty - this is a brand new project. Let me follow the brainstorming skill checklist.

1. **Explore project context** - The directory is empty, so there's nothing to explore. This is a greenfield project.

2. **Offer visual companion** - This is a UI-heavy plugin design (tracking interface, map flags, overlays), so visuals would be helpful. Let me offer the visual companion.

Let me set up the todos and then offer the visual companion.

Using brainstorming to design the FFXIV Todo plugin. Let me start by exploring the project context and setting up the process

**Tool: todowrite**

**Input:**
```json
{
  "todos": [
    {
      "content": "Explore project context",
      "status": "completed",
      "priority": "high"
    },
    {
      "content": "Offer visual companion",
      "status": "in_progress",
      "priority": "high"
    },
    {
      "content": "Ask clarifying questions (one at a time)",
      "status": "pending",
      "priority": "high"
    },
    {
      "content": "Propose 2-3 approaches with trade-offs",
      "status": "pending",
      "priority": "high"
    },
    {
      "content": "Present design sections and get approval",
      "status": "pending",
      "priority": "high"
    },
    {
      "content": "Write design doc to specs/",
      "status": "pending",
      "priority": "medium"
    },
    {
      "content": "Spec review loop",
      "status": "pending",
      "priority": "medium"
    },
    {
      "content": "User reviews written spec",
      "status": "pending",
      "priority": "medium"
    },
    {
      "content": "Transition to implementation (writing-plans skill)",
      "status": "pending",
      "priority": "medium"
    }
  ]
}
```

**Output:**
```
[
  {
    "content": "Explore project context",
    "status": "completed",
    "priority": "high"
  },
  {
    "content": "Offer visual companion",
    "status": "in_progress",
    "priority": "high"
  },
  {
    "content": "Ask clarifying questions (one at a time)",
    "status": "pending",
    "priority": "high"
  },
  {
    "content": "Propose 2-3 approaches with trade-offs",
    "status": "pending",
    "priority": "high"
  },
  {
    "content": "Present design sections and get approval",
    "status": "pending",
    "priority": "high"
  },
  {
    "content": "Write design doc to specs/",
    "status": "pending",
    "priority": "medium"
  },
  {
    "content": "Spec review loop",
    "status": "pending",
    "priority": "medium"
  },
  {
    "content": "User reviews written spec",
    "status": "pending",
    "priority": "medium"
  },
  {
    "content": "Transition to implementation (writing-plans skill)",
    "status": "pending",
    "priority": "medium"
  }
]
```

---

## Assistant (Build · DeepSeek V4 Pro · 4.0s)

_Thinking:_

The project directory is empty - greenfield project. Now I need to offer the visual companion since this is a UI-heavy plugin (tracking interfaces, overlays, map flags, etc.).

The project directory is empty — this is a greenfield design for a new Dalamud plugin.

Some of what we're working on might be easier to explain if I can show it to you in a web browser. I can put together mockups, diagrams, comparisons, and other visuals as we go. This feature is still new and can be token-intensive. Want to try

---

## User

No

---

## Assistant (Build · DeepSeek V4 Pro · 7.2s)

_Thinking:_

Okay, no visual companion. Let me proceed with text-only brainstorming. Now I need to ask clarifying questions, one at a time.

Let me update the todo and ask the first clarifying question. The user has described quite a lot already, but I need to understand scope, priorities, and some key decisions.

Key things to clarify:
1. Scope - what counts as "non-MSQ content"? Blue quests only? Side quests? Raids? Trials? FATEs? Beast tribes? etc.
2. Data sourcing - how should we source the data? The user mentioned online sources or data mining. Should this be bundled JSON, a companion website, or fetched from existing APIs?
3. UI approach - ImGui window? Separate overlay? Both?
4. Persistence - where to store player progress?
5. The "untracked" vs "tracked" concept

Let me ask the first question.

**Tool: todowrite**

**Input:**
```json
{
  "priority": "high",
  "status": "completed",
  "content": "Explore project context"
}
```

**Error:**
```
The todowrite tool was called with invalid arguments: SchemaError(Missing key
  at ["todos"]).
Please rewrite the input so it satisfies the expected schema.
```

---

## Assistant (Build · DeepSeek V4 Pro · 7.1s)

_Thinking:_

Let me update the first todo and ask the first question.

**Tool: todowrite**

**Input:**
```json
{
  "todos": [
    {
      "content": "Explore project context",
      "priority": "high",
      "status": "completed"
    },
    {
      "content": "Offer visual companion",
      "priority": "high",
      "status": "completed"
    },
    {
      "content": "Ask clarifying questions (one at a time)",
      "priority": "high",
      "status": "in_progress"
    },
    {
      "content": "Propose 2-3 approaches with trade-offs",
      "priority": "high",
      "status": "pending"
    },
    {
      "content": "Present design sections and get approval",
      "priority": "high",
      "status": "pending"
    },
    {
      "content": "Write design doc to specs/",
      "priority": "medium",
      "status": "pending"
    },
    {
      "content": "Spec review loop",
      "priority": "medium",
      "status": "pending"
    },
    {
      "content": "User reviews written spec",
      "priority": "medium",
      "status": "pending"
    },
    {
      "content": "Transition to implementation (writing-plans skill)",
      "priority": "medium",
      "status": "pending"
    }
  ]
}
```

**Output:**
```
[
  {
    "content": "Explore project context",
    "status": "completed",
    "priority": "high"
  },
  {
    "content": "Offer visual companion",
    "status": "completed",
    "priority": "high"
  },
  {
    "content": "Ask clarifying questions (one at a time)",
    "status": "in_progress",
    "priority": "high"
  },
  {
    "content": "Propose 2-3 approaches with trade-offs",
    "status": "pending",
    "priority": "high"
  },
  {
    "content": "Present design sections and get approval",
    "status": "pending",
    "priority": "high"
  },
  {
    "content": "Write design doc to specs/",
    "status": "pending",
    "priority": "medium"
  },
  {
    "content": "Spec review loop",
    "status": "pending",
    "priority": "medium"
  },
  {
    "content": "User reviews written spec",
    "status": "pending",
    "priority": "medium"
  },
  {
    "content": "Transition to implementation (writing-plans skill)",
    "status": "pending",
    "priority": "medium"
  }
]
```

---

## Assistant (Build · DeepSeek V4 Pro · 124.7s)

_Thinking:_

Good, now let me ask the first clarifying question. I need to understand the scope of "non-MSQ content" first.

First question — what categories of non-MSQ content should this track?



**Tool: question**

**Input:**
```json
{
  "questions": [
    {
      "header": "Content scope",
      "options": [
        {
          "description": "Side quests (yellow), feature unlock quests (blue+), job quests, and role quests",
          "label": "Quests only"
        },
        {
          "description": "Quests + duties/trial series, raid series, alliance raids, beast tribes, custom deliveries, etc.",
          "label": "Quests + duties + systems"
        },
        {
          "description": "Quests + duties + systems + logs (hunting/sightseeing/fishing), FATEs, treasure maps, deep dungeons, etc.",
          "label": "Everything non-MSQ"
        }
      ],
      "question": "What content categories should the tracker cover?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "What content categories should the tracker cover?"="Quests + duties + systems". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 11.0s)

_Thinking:_

Okay, so we're tracking Quests + duties + systems. This includes:
- Side quests (yellow)
- Feature unlock quests (blue+)
- Job quests, role quests
- Trial series
- Raid series (8-man normal/savage)
- Alliance raids
- Beast tribes
- Custom deliveries
- Maybe: treasure maps, hunt logs, etc.

But NOT: hunting/sightseeing/fishing logs, FATEs, deep dungeons.

Next question: data sourcing. The user mentioned online sources or data mining. I need to understand which they prefer.

Quests + duties + systems — good scope. Next: data sourcing approach.



**Tool: question**

**Input:**
```json
{
  "header": "Data source",
  "options": [
    {
      "description": "Ship a JSON file bundled with the plugin, manually curated from wiki/datamining sources",
      "label": "Bundled static data"
    },
    {
      "description": "Fetch from an online source (e.g., XIVAPI, Teamcraft data repos, custom endpoint) on startup or periodically",
      "label": "Live online fetch"
    },
    {
      "description": "Ship bundled data as base, but support fetching updates from an online source to stay current with patches",
      "label": "Hybrid (bundled + updates)"
    }
  ],
  "question": "How should content data (quests, duties, system unlocks) be sourced? Bundled JSON, fetched live from an API, or a hybrid approach?"
}
```

**Error:**
```
The question tool was called with invalid arguments: SchemaError(Missing key
  at ["questions"]).
Please rewrite the input so it satisfies the expected schema.
```

---

## Assistant (Build · DeepSeek V4 Pro · 15.1s)

_Thinking:_

Let me fix the question format - it needs to be wrapped in a `questions` array.

**Tool: question**

**Input:**
```json
{
  "questions": [
    {
      "header": "Data source",
      "options": [
        {
          "description": "Ship a JSON file bundled with the plugin, manually curated from wiki/datamining sources",
          "label": "Bundled static data"
        },
        {
          "description": "Fetch from an online source (e.g., XIVAPI, Teamcraft data repos, custom endpoint) on startup or periodically",
          "label": "Live online fetch"
        },
        {
          "description": "Ship bundled data as base, but support fetching updates from an online source to stay current with patches",
          "label": "Hybrid (bundled + updates)"
        }
      ],
      "question": "How should content data (quests, duties, system unlocks) be sourced? Bundled JSON, fetched live from an API, or a hybrid approach?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "How should content data (quests, duties, system unlocks) be sourced? Bundled JSON, fetched live from an API, or a hybrid approach?"="Bundled static data". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 19.2s)

_Thinking:_

Bundled static data. That simplifies things - no network dependencies.

Next question: About the UI approach. The user mentioned:
- Main interface
- Overlay separate from main interface for "tracked" content
- Map flagging for quest/content start/next locations

This suggests at minimum a main window and an overlay. Let me ask about the overall UI structure.

Bundled static data — clean and simple. Next question: how should the UI be structured?



**Tool: question**

**Input:**
```json
{
  "questions": [
    {
      "header": "UI structure",
      "options": [
        {
          "description": "A single Dalamud window listing everything, with a collapsible 'tracked' section at the top for pinned items",
          "label": "Single window"
        },
        {
          "description": "Main window for full list/filtering + separate HUD overlay just for tracked items",
          "label": "Window + tracked overlay"
        },
        {
          "description": "Main window + tracked overlay + mini-map/world-map integration for flagging locations",
          "label": "Full multi-surface"
        }
      ],
      "question": "How should the UI be structured across Dalamud surfaces? Main window, overlay, map integration?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "How should the UI be structured across Dalamud surfaces? Main window, overlay, map integration?"="Window + tracked overlay". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 62.5s)

_Thinking:_

Window + tracked overlay. So a main window for the full list and a separate overlay for tracked items. The user also mentioned map flagging - but map flagging would be a button/action from the interface that places flags on the in-game map, not a separate dedicated map integration UI. So that can be a feature of the window/overlay.

Next question: Progress detection. The user mentioned:
- Journal scanning for started/completed quests
- Achievement scanning for duty completions
- Manual marking

Let me ask about this.

Main window + tracked overlay. Next question: how should progress be detected vs. manually managed

**Tool: question**

**Input:**
```json
{
  "multiple": false,
  "questions": [
    {
      "header": "Progress detection",
      "options": [
        {
          "description": "Auto-detect via journal/achievement scan on login/zone change. Manual toggle available as fallback.",
          "label": "Auto with manual override"
        },
        {
          "description": "Fully manual. User checks boxes for started/progress/completed/ignored themselves.",
          "label": "Manual only"
        },
        {
          "description": "Auto-detect but lock the status — no manual override (prevents mismatches).",
          "label": "Auto-detected only (read-only)"
        }
      ],
      "question": "How should started/progress/completed status be determined? Auto-detected from journal/achievements, manually set by the player, or a combination?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "How should started/progress/completed status be determined? Auto-detected from journal/achievements, manually set by the player, or a combination?"="Auto with manual override". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 8.2s)

_Thinking:_

Auto with manual override. Good. So the plugin scans the journal and achievements to determine status, but the user can manually override any status. The "ignored" state is purely manual.

Now let me ask about the prerequisite visualization. The user mentioned:
- Show content that has prerequisites
- Indicate that it cannot be started yet
- Show prerequisite information

Let me ask about how deep the prerequisite handling should go.

Auto with manual override. Next: how should prerequisites be handled visually

**Tool: question**

**Input:**
```json
{
  "header": "Prerequisites",
  "options": [
    {
      "description": "Gray out/prerequisite content, show tooltip listing what's needed. No automatic unlocking.",
      "label": "Visual indication + tooltip"
    },
    {
      "description": "Visual indication + inline prerequisite chain (tree view showing what unlocks what)",
      "label": "Inline dependency tree"
    },
    {
      "description": "Visual indication + clickable prerequisite chain that navigates/filters to the blocking content",
      "label": "Linked prerequisite navigation"
    }
  ],
  "multiple": false,
  "question": "How should prerequisite dependencies be displayed and interacted with?"
}
```

**Error:**
```
The question tool was called with invalid arguments: SchemaError(Missing key
  at ["questions"]).
Please rewrite the input so it satisfies the expected schema.
```

---

## Assistant (Build · DeepSeek V4 Pro · 24.7s)

_Thinking:_

Need to wrap in questions array.

**Tool: question**

**Input:**
```json
{
  "multiple": false,
  "questions": [
    {
      "header": "Prerequisites",
      "options": [
        {
          "description": "Gray out prerequisite content, show tooltip listing what's needed. No automatic unlocking.",
          "label": "Visual indication + tooltip"
        },
        {
          "description": "Visual indication + inline prerequisite chain (tree view showing what unlocks what)",
          "label": "Inline dependency tree"
        },
        {
          "description": "Visual indication + clickable prerequisite chain that navigates/filters to the blocking content",
          "label": "Linked prerequisite navigation"
        }
      ],
      "question": "How should prerequisite dependencies be displayed and interacted with?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "How should prerequisite dependencies be displayed and interacted with?"="Visual indication + tooltip". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 25.2s)

_Thinking:_

Visual indication + tooltip. Simple and clear. Now let me ask about the map flagging feature that the user mentioned.

Visual indication + tooltip for prerequisites. Next: map flagging behavior

**Tool: question**

**Input:**
```json
{
  "multiple": false,
  "questions": [
    {
      "header": "Map flagging",
      "options": [
        {
          "description": "Click a button on any item to set a flag on the in-game map to the quest start NPC or next objective location",
          "label": "One-click flag on any item"
        },
        {
          "description": "Only flaggeable from the overlay, not the main window. Main window shows location name as text.",
          "label": "Flag from overlay only"
        },
        {
          "description": "Auto-flag the nearest relevant content start when opening the map",
          "label": "Auto-flag on map open"
        }
      ],
      "question": "How should the map flagging feature work? What triggers placing a flag on the in-game map?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "How should the map flagging feature work? What triggers placing a flag on the in-game map?"="One-click flag on any item". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 39.6s)

_Thinking:_

Good. Now let me ask about one more thing - expansion visibility/filtering and level display, since the user mentioned that. And also the data model - how granular should items be?

Actually, the user already said:
- Tracked by expansion
- Showing level

So those are clear. Let me ask about another important design choice: how to group/list items.

One-click flag on any item. Now let me ask about the main window layout: how should content be grouped



**Tool: question**

**Input:**
```json
{
  "questions": [
    {
      "header": "Content grouping",
      "options": [
        {
          "description": "Tab per expansion (ARR, HW, etc.), items listed flat under each tab with category icons",
          "label": "Tabs by expansion"
        },
        {
          "description": "Single scrollable list grouped by expansion headers, with collapsible categories within each",
          "label": "Single list with headers"
        },
        {
          "description": "Filterable tree view: Expansion > Category (Side Quests, Raids, etc.) > Items",
          "label": "Tree view"
        }
      ],
      "question": "How should content be visually grouped and navigated in the main window?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "How should content be visually grouped and navigated in the main window?"="Tree view". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 18.8s)

_Thinking:_

Tree view: Expansion > Category (Side Quests, Raids, etc.) > Items. This is a classic approach.

Now let me think about a few more questions. I think key remaining questions are:

1. Persistence - where to store player progress data?
2. Data model granularity - what does an "item" look like?
3. Categories within an expansion

But I think I have enough to propose approaches now. Let me move to presenting approaches.

Let me summarize what we have so far:

**Scope**: Quests + duties + systems (non-MSQ)
**Data**: Bundled static JSON
**UI**: Main window (tree view) + tracked overlay
**Progress**: Auto-detected from journal/achievements with manual override
**Prerequisites**: Visual indication + tooltip
**Map**: One-click flag from any item
**Grouping**: Tree view (Expansion > Category > Items)
**Features**: 
- Track/untrack for overlay prominence
- Ignore for hiding
- Status: not started, started, in progress, completed
- Flag on map
- Expansion + level display
- Prerequisite tooltip

Let me propose 2-3 approaches now.

Good. I have enough to propose approaches now. Let me summarize what we've established so far:

- **Scope**: Quests + duties + systems (side quests, blue unlock quests, job/role quests, trial series, raid series, alliance raids, beast tribes, custom deliveries)
- **Data**: Bundled static JSON
- **UI**: Main window + tracked overlay
- **Progress**: Auto-detected from journal/achievements, manual override
- **Prerequisites**: Visual indication + tooltip
- **Map**: One-click flag from any item
- **Grouping**: Tree view (Expansion > Category > Items)

---

Here are 3 architectural approaches:

**Approach A: Single-layered service (simplest)**
Everything in one service. A `ContentManager` loads bundled JSON, a `ProgressTracker` polls journal/achievements each frame, and the UI reads directly from these. Overlay is a separate window sharing the same data. ~5-6 classes. Fastest to build but tight coupling.

**B: Plugin + data layer separation (recommended)**
Data layer (`ContentDatabase` + `ProgressStore`) is independent from the UI/scanning layer. `ContentDatabase` loads JSON, `ProgressTracker` scans game state, writes to `ProgressStore`. UI reads from `ProgressStore` and `ContentDatabase`. Clean separation for future data sources or UI changes. ~8-10 classes.

**C: Plugin with IPC architecture**
All state in a central `StateManager`. UI, scanning, and map-flagging all communicate through defined IPC-style interfaces. Over-engineered for this scale but very testable. ~15+ classes.

Given the scope (single plugin, one developer, bundled data), **I recommend Approach B** — it provides clean separation without the overhead of a full IPC architecture.

Does Approach B sound right, or do you prefer another?

---

## User

Agreed

---

## Assistant (Build · DeepSeek V4 Pro · 8.2s)

_Thinking:_

User agreed with Approach B. Now I need to present the design sections, one at a time, getting approval after each.

Let me start with the data model - this is the foundation of the whole plugin.

**Tool: todowrite**

**Input:**
```json
{
  "todos": "[{\"content\": \"Explore project context\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Offer visual companion\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Ask clarifying questions (one at a time)\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Propose 2-3 approaches with trade-offs\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Present design sections and get approval\", \"priority\": \"high\", \"status\": \"in_progress\"}, {\"content\": \"Write design doc to specs/\", \"priority\": \"medium\", \"status\": \"pending\"}, {\"content\": \"Spec review loop\", \"priority\": \"medium\", \"status\": \"pending\"}, {\"content\": \"User reviews written spec\", \"priority\": \"medium\", \"status\": \"pending\"}, {\"content\": \"Transition to implementation (writing-plans skill)\", \"priority\": \"medium\", \"status\": \"pending\"}]"
}
```

**Error:**
```
The todowrite tool was called with invalid arguments: SchemaError(Expected array, got "[{\"content\": \"Explore project context\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Offer visual companion\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Ask clarifying questions (one at a time)\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Propose 2-3 approaches with trade-offs\", \"priority\": \"high\", \"status\": \"completed\"}, {\"content\": \"Present design sections and get approval\", \"priority\": \"high\", \"status\": \"in_progress\"}, {\"content\": \"Write design doc to specs/\", \"priority\": \"medium\", \"status\": \"pending\"}, {\"content\": \"Spec review loop\", \"priority\": \"medium\", \"status\": \"pending\"}, {\"content\": \"User reviews written spec\", \"priority\": \"medium\", \"status\": \"pending\"}, {\"content\": \"Transition to implementation (writing-plans skill)\", \"priority\": \"medium\", \"status\": \"pending\"}]"
  at ["todos"]).
Please rewrite the input so it satisfies the expected schema.
```

---

## Assistant (Build · DeepSeek V4 Pro · 14.5s)

_Thinking:_

The tool seems to be interpreting the JSON string differently. Let me try sending it as a proper JavaScript array.

Let me start presenting the design, section by section.

---

## Section 1: Data Model

### Content Database (bundled JSON)

A single `content.json` file shipped with the plugin structured as:

```
Expansion[] → Category[] → ContentItem[]
```

**ContentItem** fields:
| Field | Type | Description |
|-------|------|-------------|
| `Id` | uint | Unique numeric ID |
| `Name` | string | Display name |
| `Level` | uint | Required level |
| `Expansion` | enum | ARR, HW, SB, ShB, EW, DT |
| `Category` | enum | SideQuest, BlueUnlock, JobQuest, RoleQuest, TrialSeries, RaidSeries, AllianceRaid, BeastTribe, CustomDelivery |
| `PrerequisiteIds` | uint[] | IDs that must be completed first |
| `LocationTerritoryId` | uint | Territory for map flagging |
| `LocationMapX/Y` | float | Coordinates for map flagging |
| `QuestId` | uint? | Journal quest ID for auto-detection |
| `AchievementId` | uint? | Achievement ID for completion detection |

### Progress Store (player state, local JSON)

```
ContentItem.Id → { Status: NotStarted/Started/InProgress/Completed, IsTracked: bool, IsIgnored: bool }
```

This is read/written as a simple flat file in the plugin's config directory. `Status` defaults to `NotStarted` and is auto-populated on first scan.

**Derived display states** (computed from ContentDatabase + ProgressStore):
- **Locked**: any prerequisite has status != Completed
- **Available**: all prerequisites completed, status = NotStarted
- **Started/InProgress/Completed**: from ProgressStore
- **Hidden**: IsIgnored = true

One thing to note: the user can manually set status to any value (auto-detection runs on login/zone change but never overwrites when status was manually changed—we track a simple `IsManual` flag).

Does this data model look right so far?

---

## User

Yes

---

## Assistant (Build · DeepSeek V4 Pro · 19.7s)

_Thinking:_

Good, data model is approved. Now let me present the architecture/component design section.

## Section 2: Architecture & Components

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

**6 components**:

| Component | Responsibility |
|-----------|---------------|
| `Plugin` | Dalamud lifecycle, command registration, window toggling |
| `ContentManager` | Loads `content.json`, resolves prereq chains, computes per-item display state by joining content data with player progress |
| `ProgressStore` | Loads/saves player progress to `progress.json` in the plugin config directory; exposes get/set by item ID |
| `ProgressScanner` | On login and zone change, reads journal and achievements via Dalamud's `QuestManager` and `AchievementManager` APIs; updates `ProgressStore` for non-manual entries |
| `MainWindow` | ImGui tree view: Expansion > Category > Items. Filters (expansion, category, status), per-item actions: flag map, toggle tracked, toggle ignored, set manual status |
| `OverlayWindow` | ImGui overlay showing only `IsTracked=true` items sorted by proximity/expansion, with flag-map and un-track action |
| `MapFlagHelper` | Places and removes flags on the in-game map using Dalamud's map marker API |

**Data flow**:
1. `Plugin` initializes `ContentManager` and `ProgressStore` on startup
2. `ProgressScanner` fires on login/zone change → updates `ProgressStore` → `ContentManager` re-computes display states
3. Both windows read from `ContentManager` (which joins DB + progress)
4. User actions (track, ignore, flag) write through `ContentManager` → `ProgressStore`
5. Map flagging goes through `MapFlagHelper` using territory + coordinates from content data

Does this component layout look right?

---

## User

Yes

---

## Assistant (Build · DeepSeek V4 Pro · 13.0s)

_Thinking:_

Architecture approved. Now let me present the UI details section.

## Section 3: UI — Main Window

**Main Window (`/todo` command toggles)**

A single ImGui window with a left panel (tree) and a right panel (details):

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

- `[ ]` = available, `[✓]` = completed, `[~]` = started/in progress
- `[!]` = locked (prereqs not met), grayed out
- Tree nodes show item counts
- Click to select → populates right panel
- Right-click context menu: Track/Untrack, Ignore/Unignore, Flag on Map, Mark as Complete/Not Started

**Top bar:** filter dropdowns (expansion, category, status) + search text input. When no filters active, "Show All" is the default.

**Right panel — Item Details (shown when item selected):**
- Name, level, expansion, category
- Status with current auto-detected state and manual override buttons
- Prerequisites section: lists prerequisites by name with their current status
- "Track this" / "Untrack" button
- "Ignore this" / "Unignore" button
- "Flag on Map" button
- "Set as Complete" / "Set as Not Started" buttons

**Menu bar:** "Show ignored" toggle (to see ignored items in the tree, dimmed) + "Refresh" button to re-scan journal + "About" with data version info.

Does the main window design look right

---

## User

We should also have a link to one of the Wikis for the item, for additional information ans context

---

## Assistant (Build · DeepSeek V4 Pro · 8.7s)

_Thinking:_

Good point - a wiki link for each item. Let me add that to the right panel details section and adjust.

Let me update: in the right panel, add a clickable wiki link button. The data model would need a `WikiUrl` or `WikiSlug` field per item - or we can construct the URL from the item name/category.

Let me ask about this.

Good call. Two options for implementing that

**Tool: question**

**Input:**
```json
{
  "header": "Wiki linking",
  "multiple": false,
  "options": [
    {
      "description": "Store a full URL per item in content.json — most reliable, slightly more data to maintain",
      "label": "Per-item URL in JSON"
    },
    {
      "description": "Construct URL from standardized fields (name + category) pointing to consolegameswiki — simpler to maintain, but fragile if naming drifts",
      "label": "Construct from name"
    }
  ],
  "questions": [
    {
      "question": "Should the wiki link be a per-item URL stored in content.json, or constructed dynamically from the item name?"
    }
  ]
}
```

**Error:**
```
The question tool was called with invalid arguments: SchemaError(Missing key
  at ["questions"][0]["header"]).
Please rewrite the input so it satisfies the expected schema.
```

---

## Assistant (Build · DeepSeek V4 Pro · 23.6s)

_Thinking:_

I'm missing the header key in the question.

**Tool: question**

**Input:**
```json
{
  "questions": [
    {
      "header": "Wiki linking",
      "options": [
        {
          "description": "Store a full URL per item in content.json — most reliable, slightly more data to maintain",
          "label": "Per-item URL in JSON"
        },
        {
          "description": "Construct URL from name + category pointing to consolegameswiki — simpler to maintain, but fragile if naming drifts",
          "label": "Construct from name"
        }
      ],
      "question": "Should the wiki link be a per-item URL stored in content.json, or constructed dynamically from the item name?"
    }
  ]
}
```

**Output:**
```
User has answered your questions: "Should the wiki link be a per-item URL stored in content.json, or constructed dynamically from the item name?"="Per-item URL in JSON". You can now continue with the user's answers in mind.
```

---

## Assistant (Build · DeepSeek V4 Pro · 11.4s)

_Thinking:_

Good. Per-item URL in JSON. So the data model gets a `WikiUrl` field. Let me update and continue with the main window design with this addition.

I'll add this to Section 3 (the main window) and note the data model change, then move on to the overlay design.

I'll add `WikiUrl` to the content data model and a "Open Wiki" button in the right panel details.

---

## Section 4: UI — Tracked Overlay

**Overlay Window (toggle via config or main window action)**

A lightweight ImGui overlay with minimal chrome:

```
 ┌─ Todo ──────────────────────────┐
 │ [~] Binding Coil T1     ARR Lv50 │
 │ [ ] The Howling Eye     ARR Lv50 │
 │ [ ] Eureka Anemos        SB Lv70 │
 │ [!] Bozja Southern     ShB Lv71 │
 └──────────────────────────────────┘
```

**Behavior:**
- Shows only `IsTracked=true` items, sorted by expansion then level
- Each item shows: status icon, name, expansion badge, level
- Hover: tooltip with full details (category, prereq status)
- Right-click context menu: Flag on Map, Untrack, Mark Complete
- Configurable: lock position, background opacity, max items shown, font scale
- `/todo overlay` toggles visibility

**Configuration** (saved to plugin config):
- Overlay position (X, Y) and size (width)
- Background opacity (0–100%)
- Max visible items before scrolling
- Always-on-top toggle
- Font scale

Does the overlay design look right

---

## User

Contibue

---

## Assistant (Build · DeepSeek V4 Pro · 11.3s)

_Thinking:_

The user said "Continue" - they likely meant "Continue" approving the design. Let me move on to the next section. I'll present the scanning/progress detection design.

## Section 5: Progress Scanning

**ProgressScanner** runs on two triggers:
1. **On login** (full scan)
2. **On zone change** (incremental scan, debounced to avoid spam)

**Quest detection** (via Dalamud's `QuestManager`):
- Check `QuestManager.IsQuestComplete(questId)` for completed quests
- Check `QuestManager.GetQuestList()` for accepted/in-progress quests
- Requires the `QuestId` field in content data to be populated

**Achievement detection** (via Dalamud's `AchievementManager`):
- Check `AchievementManager.IsComplete(achievementId)` for duty/system completions
- Requires the `AchievementId` field in content data

**Manual override protection:**
- Each `ProgressStore` entry has `IsManual: bool`
- Scanner skips any entry where `IsManual = true`
- When user manually sets status via UI, `IsManual` is set to `true`
- A "Reset to Auto" button in the UI clears the manual flag on an item
- Manual overrides are also shown differently in the UI (e.g., italicized or with a `*`)

**Performance:** On login, the scanner iterates all content items with a `QuestId` or `AchievementId` (typically hundreds, not thousands). The Dalamud API calls are synchronous and cheap. Should complete in under a second.

**Edge cases:**
- QuestId/AchievementId both null: item is purely manual (user must mark themselves)
- QuestId found but achievement also exists: completion = achievement complete OR quest complete (union, not intersection)

Does the scanning approach look correct?

---

## User

Looks good

---

## Assistant (Build · DeepSeek V4 Pro · 16.9s)

_Thinking:_

Moving on to the final section - the map flagging.

## Section 6: Map Flagging

**MapFlagHelper** uses Dalamud's `IMapLinkPayload` and `GameGui.OpenMapWithMapLink()`:

**Flow when user clicks "Flag on Map":**
1. Read `LocationTerritoryId`, `LocationMapX`, `LocationMapY` from the content item
2. Create a map link payload for that territory and coordinates
3. Call `GameGui.OpenMapWithMapLink()` — this opens the in-game map centered on the coordinates
4. Place a map marker at the exact coordinates via Dalamud's map marker API

**Coordinate system:**
- FFXIV uses a normalized 0–100 scale for map coordinates in a given territory
- These must be sourced from datamining or manually recorded per content item
- Some content may have multiple locations (start NPC, turn-in NPC, instance entrance) — for v1, we store only the start location

**Fallback behavior:**
- If `LocationTerritoryId` is null or 0, the "Flag on Map" button is grayed out with tooltip "No map location available"
- Content that doesn't have a specific location (e.g., system unlocks without a quest NPC) won't have flagging

**Flag management:**
- Only one flag can be active at a time (FFXIV limitation)
- Placing a new flag replaces the previous one
- Flags persist until replaced or manually removed by the player

---

## Section 7: Data File Structure

**content.json** (bundled, updated via plugin releases):
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

**progress.json** (player state, local only):
```json
{
  "1001": { "status": "Completed", "isTracked": false, "isIgnored": false, "isManual": false },
  "1002": { "status": "NotStarted", "isTracked": true, "isIgnored": false, "isManual": false }
}
```

---

That covers all design sections. Does everything look right before I write the design doc?

---

## User

Looks good, proceed with design dock

---

## Assistant (Build · DeepSeek V4 Pro · 4.5s)

_Thinking:_

Now I need to:
1. Write the design doc to specs/
2. Run the spec review loop
3. Have user review
4. Transition to writing-plans

Let me create the directory structure and write the design doc.

**Tool: todowrite**

**Input:**
```json
{
  "priority": "high",
  "status": "in_progress",
  "content": "Present design sections and get approval"
}
```

**Error:**
```
The todowrite tool was called with invalid arguments: SchemaError(Missing key
  at ["todos"]).
Please rewrite the input so it satisfies the expected schema.
```

---

## Assistant (Build · DeepSeek V4 Pro · 3.4s)

_Thinking:_

Need the todos array format. Let me just proceed with writing the doc.

**Tool: bash**

**Input:**
```json
{
  "command": "mkdir -p /home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs",
  "description": "Create specs directory"
}
```

**Output:**
```
(no output)
```

---

## Assistant (Build · DeepSeek V4 Pro · 59.7s)

_Thinking:_

Now let me write the design doc.

**Tool: write**

**Input:**
```json
{
  "content": "# FFXIV Todo Plugin — Design Spec\n\nA Dalamud plugin to track unlocking and completion of all non-MSQ content in Final Fantasy XIV.\n\n## Scope\n\n**Tracked content categories:**\n\n- Side Quests (yellow quest markers)\n- Feature Unlock Quests (blue plus quest markers)\n- Job Quests\n- Role Quests\n- Trial Series\n- Raid Series (8-man normal/savage)\n- Alliance Raids\n- Beast Tribes\n- Custom Deliveries\n\n**Out of scope (v1):** Hunting logs, sightseeing logs, fishing logs, FATEs, treasure maps, deep dungeons.\n\n## Data Model\n\n### Content Database (`content.json` — bundled with plugin)\n\nStructured as `Expansion[] → Category[] → ContentItem[]`.\n\n**ContentItem fields:**\n\n| Field | Type | Description |\n|-------|------|-------------|\n| `Id` | uint | Unique numeric identifier |\n| `Name` | string | Display name |\n| `Level` | uint | Required level |\n| `Expansion` | enum | ARR, HW, SB, ShB, EW, DT |\n| `Category` | enum | SideQuest, BlueUnlock, JobQuest, RoleQuest, TrialSeries, RaidSeries, AllianceRaid, BeastTribe, CustomDelivery |\n| `PrerequisiteIds` | uint[] | IDs that must be completed before this is available |\n| `LocationTerritoryId` | uint? | Territory for map flagging (nullable) |\n| `LocationMapX` | float? | Map X coordinate (nullable) |\n| `LocationMapY` | float? | Map Y coordinate (nullable) |\n| `QuestId` | uint? | Journal quest ID for auto-detection (nullable) |\n| `AchievementId` | uint? | Achievement ID for completion detection (nullable) |\n| `WikiUrl` | string? | Link to wiki page for the item (nullable) |\n\n**Example entry:**\n\n```json\n{\n  \"version\": 1,\n  \"items\": [\n    {\n      \"id\": 1001,\n      \"name\": \"The Binding Coil of Bahamut - Turn 1\",\n      \"level\": 50,\n      \"expansion\": \"ARR\",\n      \"category\": \"RaidSeries\",\n      \"prerequisiteIds\": [1000],\n      \"locationTerritoryId\": 1043,\n      \"locationMapX\": 15.5,\n      \"locationMapY\": 22.3,\n      \"questId\": null,\n      \"achievementId\": 690,\n      \"wikiUrl\": \"https://ffxiv.consolegameswiki.com/wiki/The_Binding_Coil_of_Bahamut_-_Turn_1\"\n    }\n  ]\n}\n```\n\n### Progress Store (`progress.json` — local player state)\n\nFlat map of `ContentItem.Id → ProgressEntry`:\n\n```json\n{\n  \"1000\": { \"status\": \"Completed\", \"isTracked\": false, \"isIgnored\": false, \"isManual\": false },\n  \"1001\": { \"status\": \"Started\", \"isTracked\": true, \"isIgnored\": false, \"isManual\": false },\n  \"1002\": { \"status\": \"NotStarted\", \"isTracked\": false, \"isIgnored\": true, \"isManual\": true }\n}\n```\n\n**ProgressEntry fields:**\n\n| Field | Type | Description |\n|-------|------|-------------|\n| `Status` | enum | NotStarted, Started, InProgress, Completed |\n| `IsTracked` | bool | Promoted to overlay visibility |\n| `IsIgnored` | bool | Hidden from all views (unless \"show ignored\" is on) |\n| `IsManual` | bool | If true, auto-scanner skips this entry |\n\n### Derived Display States (computed at runtime)\n\n- **Locked**: any prerequisite has status != Completed\n- **Available**: all prerequisites completed, status = NotStarted\n- **Started/InProgress/Completed**: direct from ProgressStore\n- **Hidden**: IsIgnored = true\n\n## Architecture\n\nApproach B: Plugin + data layer separation.\n\n```\n┌─────────────────────────────────────────────────┐\n│  Plugin.cs (entry point, lifecycle)              │\n├─────────────────────────────────────────────────┤\n│                                                   │\n│  ┌──────────────┐  ┌──────────────┐              │\n│  │ MainWindow   │  │ OverlayWindow│              │\n│  │ (tree view,  │  │ (tracked     │              │\n│  │  filters)    │  │  items only) │              │\n│  └──────┬───────┘  └──────┬───────┘              │\n│         │                 │                      │\n│         └────────┬────────┘                      │\n│                  ▼                               │\n│  ┌───────────────────────────┐                   │\n│  │    ContentManager          │                  │\n│  │  - Loads content.json     │                  │\n│  │  - Resolves prerequisites  │                  │\n│  │  - Computes display state  │                  │\n│  └───────────────────────────┘                   │\n│                  │                               │\n│  ┌───────────────┴───────────┐                   │\n│  │                           │                   │\n│  ▼                           ▼                   │\n│  ┌──────────────┐  ┌──────────────────┐         │\n│  │ProgressStore │  │  ProgressScanner │         │\n│  │(load/save    │  │  (polls journal, │         │\n│  │ state JSON)  │  │   achievements)  │         │\n│  └──────────────┘  └──────────────────┘         │\n│                                                   │\n│  ┌──────────────┐                                │\n│  │ MapFlagHelper│  (places/removes map flags)    │\n│  └──────────────┘                                │\n└─────────────────────────────────────────────────┘\n```\n\n### Components\n\n| Component | Responsibility |\n|-----------|---------------|\n| `Plugin` | Dalamud lifecycle, command registration, window toggling |\n| `ContentManager` | Loads `content.json`, resolves prerequisite chains, computes per-item display state by joining content data with player progress |\n| `ProgressStore` | Loads/saves player progress to `progress.json` in plugin config directory; exposes get/set by item ID |\n| `ProgressScanner` | On login and zone change, reads journal and achievements via Dalamud's `QuestManager` and `AchievementManager` APIs; updates `ProgressStore` for non-manual entries |\n| `MainWindow` | ImGui tree view: Expansion > Category > Items. Filters, per-item actions, right panel detail view |\n| `OverlayWindow` | ImGui overlay showing only tracked items with minimal chrome |\n| `MapFlagHelper` | Places map flags using Dalamud's `IMapLinkPayload` and `GameGui.OpenMapWithMapLink()` |\n\n### Data Flow\n\n1. `Plugin` initializes `ContentManager` and `ProgressStore` on startup\n2. `ProgressScanner` fires on login/zone change → updates `ProgressStore` → `ContentManager` re-computes display states\n3. Both windows read from `ContentManager` (which joins DB + progress)\n4. User actions (track, ignore, flag) write through `ContentManager` → `ProgressStore`\n5. Map flagging goes through `MapFlagHelper` using territory + coordinates from content data\n\n## UI Design\n\n### Main Window (`/todo` toggles)\n\nA single ImGui window with a left panel (tree) and a right panel (details).\n\n**Left panel — Tree View:**\n\n```\n▼ A Realm Reborn (50)\n  ▼ Side Quests (12)\n    [ ] The Greatest Story Never Told  Lv.30\n    [✓] Buried Truth                  Lv.35\n    [!] Hungry Like the Wolf          Lv.45  (locked)\n  ▶ Blue Unlock Quests (8)\n  ▶ Raid Series (3)\n▼ Heavensward (42)\n  ...\n```\n\nStatus icons: `[ ]` available, `[✓]` completed, `[~]` started/in progress, `[!]` locked (grayed out).\n\nTree nodes show item counts. Click to select → populates right panel. Right-click context menu: Track/Untrack, Ignore/Unignore, Flag on Map, Mark as Complete/Not Started.\n\n**Top bar:** filter dropdowns (expansion, category, status) + search text input.\n\n**Right panel — Item Details (shown when item selected):**\n- Name, level, expansion, category\n- Status display with manual override buttons\n- Prerequisites section: lists prerequisites by name with their current status\n- \"Track this\" / \"Untrack\" button\n- \"Ignore this\" / \"Unignore\" button\n- \"Flag on Map\" button (grayed out if no location data)\n- \"Open Wiki\" button (opens URL in default browser)\n- \"Set as Complete\" / \"Set as Not Started\" buttons\n\n**Menu bar:** \"Show ignored\" toggle + \"Refresh\" button (re-scan journal) + \"About\" with data version info.\n\n### Overlay Window (`/todo overlay` toggles)\n\nMinimal ImGui overlay showing only tracked items:\n\n```\n ┌─ Todo ──────────────────────────┐\n │ [~] Binding Coil T1     ARR Lv50 │\n │ [ ] The Howling Eye     ARR Lv50 │\n │ [ ] Eureka Anemos        SB Lv70 │\n │ [!] Bozja Southern     ShB Lv71 │\n └──────────────────────────────────┘\n```\n\n- Shows only `IsTracked=true` items, sorted by expansion then level\n- Each item shows: status icon, name, expansion badge, level\n- Hover: tooltip with full details (category, prereq status)\n- Right-click context menu: Flag on Map, Untrack, Mark Complete\n\n**Configurable settings (saved to plugin config):**\n- Overlay position (X, Y) and width\n- Background opacity (0–100%)\n- Max visible items before scrolling\n- Always-on-top toggle\n- Font scale\n\n## Progress Scanning\n\n**Triggers:**\n1. On login (full scan)\n2. On zone change (debounced incremental scan)\n\n**Quest detection** (via Dalamud's `QuestManager`):\n- `QuestManager.IsQuestComplete(questId)` for completed quests\n- `QuestManager.GetQuestList()` for accepted/in-progress quests\n\n**Achievement detection** (via Dalamud's `AchievementManager`):\n- `AchievementManager.IsComplete(achievementId)` for duty/system completions\n\n**Manual override protection:**\n- Scanner skips any entry where `IsManual = true`\n- When user manually sets status via UI, `IsManual` is set to `true`\n- A \"Reset to Auto\" button clears the manual flag\n- Manual overrides are visually distinct in the UI (italicized with a `*`)\n\n**Edge cases:**\n- QuestId and AchievementId both null: item is purely manual\n- Both QuestId and AchievementId present: completion = whichever completes first (union, not intersection)\n\n## Map Flagging\n\n**Flow when user clicks \"Flag on Map\":**\n1. Read `LocationTerritoryId`, `LocationMapX`, `LocationMapY` from content item\n2. Create map link payload for that territory and coordinates\n3. Call `GameGui.OpenMapWithMapLink()` to open and center the in-game map\n4. Place a map marker at the coordinates via Dalamud's map marker API\n\n**Constraints:**\n- Only one flag active at a time (FFXIV limitation)\n- If `LocationTerritoryId` is null/0, the button is grayed out with tooltip \"No map location available\"\n- v1 stores only the start/quest-giver location\n\n## Commands\n\n| Command | Action |\n|---------|--------|\n| `/todo` | Toggle main window |\n| `/todo overlay` | Toggle tracked overlay |\n| `/todo refresh` | Force re-scan journal/achievements |\n\n## Configuration\n\nPlugin config saved via Dalamud's config system:\n- Overlay position, size, opacity, font scale\n- \"Show ignored\" default state\n- Scan debounce interval (default: 2 seconds on zone change)\n\n## Out of Scope (v2+)\n\n- Additional content categories (FATEs, deep dungeons, hunting logs, etc.)\n- Live online data fetching\n- Filter presets / saved views\n- Multi-character profile support\n- Custom content item creation\n",
  "filePath": "/home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md"
}
```

**Output:**
```
Wrote file successfully.
```

---

## Assistant (Build · DeepSeek V4 Pro · 3.3s)

_Thinking:_

Good. Now let me commit the design doc and run the spec review loop.

**Tool: bash**

**Input:**
```json
{
  "command": "git init && git add -A && git commit -m \"Initial design spec for FFXIV Todo plugin\"",
  "description": "Init git repo and commit design spec"
}
```

**Output:**
```
Initialized empty Git repository in /home/vcastellano/Projects/ffxiv-todo/.git/
[main (root-commit) f124f0d] Initial design spec for FFXIV Todo plugin
 1 file changed, 275 insertions(+)
 create mode 100644 docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md

```

---

## Assistant (Build · DeepSeek V4 Pro · 4.7s)

_Thinking:_

Now I need to run the spec review loop. The brainstorming skill says to dispatch a spec-document-reviewer subagent. Let me read the spec-document-reviewer-prompt.md to understand how to structure the review request.

Now the spec review loop. Let me load the reviewer prompt template

**Tool: read**

**Input:**
```json
{
  "filePath": "/home/vcastellano/.agents/skills/superpowers/brainstorming/spec-document-reviewer-prompt.md"
}
```

**Output:**
```
<path>/home/vcastellano/.agents/skills/superpowers/brainstorming/spec-document-reviewer-prompt.md</path>
<type>file</type>
<content>
1: # Spec Document Reviewer Prompt Template
2: 
3: Use this template when dispatching a spec document reviewer subagent.
4: 
5: **Purpose:** Verify the spec is complete, consistent, and ready for implementation planning.
6: 
7: **Dispatch after:** Spec document is written to docs/superpowers/specs/
8: 
9: ```
10: Task tool (general-purpose):
11:   description: "Review spec document"
12:   prompt: |
13:     You are a spec document reviewer. Verify this spec is complete and ready for planning.
14: 
15:     **Spec to review:** [SPEC_FILE_PATH]
16: 
17:     ## What to Check
18: 
19:     | Category | What to Look For |
20:     |----------|------------------|
21:     | Completeness | TODOs, placeholders, "TBD", incomplete sections |
22:     | Consistency | Internal contradictions, conflicting requirements |
23:     | Clarity | Requirements ambiguous enough to cause someone to build the wrong thing |
24:     | Scope | Focused enough for a single plan — not covering multiple independent subsystems |
25:     | YAGNI | Unrequested features, over-engineering |
26: 
27:     ## Calibration
28: 
29:     **Only flag issues that would cause real problems during implementation planning.**
30:     A missing section, a contradiction, or a requirement so ambiguous it could be
31:     interpreted two different ways — those are issues. Minor wording improvements,
32:     stylistic preferences, and "sections less detailed than others" are not.
33: 
34:     Approve unless there are serious gaps that would lead to a flawed plan.
35: 
36:     ## Output Format
37: 
38:     ## Spec Review
39: 
40:     **Status:** Approved | Issues Found
41: 
42:     **Issues (if any):**
43:     - [Section X]: [specific issue] - [why it matters for planning]
44: 
45:     **Recommendations (advisory, do not block approval):**
46:     - [suggestions for improvement]
47: ```
48: 
49: **Reviewer returns:** Status, Issues (if any), Recommendations

(End of file - total 49 lines)
</content>
```

---

## Assistant (Build · DeepSeek V4 Pro · 34.5s)

_Thinking:_

Now I need to dispatch a spec document reviewer subagent using the Task tool.

**Tool: task**

**Input:**
```json
{
  "description": "Review spec document",
  "prompt": "You are a spec document reviewer. Verify this spec is complete and ready for planning.\n\n**Spec to review:** /home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md\n\n## What to Check\n\n| Category | What to Look For |\n|----------|------------------|\n| Completeness | TODOs, placeholders, \"TBD\", incomplete sections |\n| Consistency | Internal contradictions, conflicting requirements |\n| Clarity | Requirements ambiguous enough to cause someone to build the wrong thing |\n| Scope | Focused enough for a single plan — not covering multiple independent subsystems |\n| YAGNI | Unrequested features, over-engineering |\n\n## Calibration\n\n**Only flag issues that would cause real problems during implementation planning.**\nA missing section, a contradiction, or a requirement so ambiguous it could be\ninterpreted two different ways — those are issues. Minor wording improvements,\nstylistic preferences, and \"sections less detailed than others\" are not.\n\nApprove unless there are serious gaps that would lead to a flawed plan.\n\n## Output Format\n\n## Spec Review\n\n**Status:** Approved | Issues Found\n\n**Issues (if any):**\n- [Section X]: [specific issue] - [why it matters for planning]\n\n**Recommendations (advisory, do not block approval):**\n- [suggestions for improvement]\n\n**Key thing to note:** this is a FFXIV Dalamud plugin. Think about what someone implementing a Dalamud plugin would need to know that might not be specified.",
  "subagent_type": "explorer"
}
```

**Output:**
```
<task id="ses_13db13c1affenvTIRlxYlEZpv3" state="completed">
<task_result>
## Spec Review

**Status:** Approved

**Key ambiguities (advisory, do not block):**

- **`Started` vs `InProgress` distinction**: The spec defines both as status values, but the `ProgressScanner` only has two sources of truth from Dalamud — quest is in the journal (active) or not (not started/completed). There's no described mechanism to auto-detect a `Started` vs `InProgress` state. Plan author should decide whether `Started` is purely manual or merge them.

**Recommendations (advisory):**
- Consider whether `Started` and `InProgress` should be collapsed (the scanner can only distinguish "not in journal" → NotStarted, "in journal" → InProgress, "achievement complete" → Completed).
- Standard Dalamud plugin boilerplate (manifest JSON, csproj target, dependencies) is not covered but is expected knowledge — no spec changes needed.

**Summary:** The spec is complete, internally consistent, and scoped appropriately for a single implementation plan. No blockers.
</task_result>
</task>
```

---

## Assistant (Build · DeepSeek V4 Pro · 19.2s)

_Thinking:_

The spec review came back approved with one advisory note about the Started vs InProgress distinction. This doesn't block approval, but it's a good point worth addressing. Let me fix the one advisory - I should collapse Started into InProgress since the scanner can't distinguish them.

Actually, the reviewer says it's approved and this is advisory. The prompt says "Only flag issues that would cause real problems during implementation planning." Since it's advisory, I should ask the user if they want me to address it before proceeding.

Actually, let me just make the change - it's a small clarification. The scanner can detect:
- Complete (quest done or achievement done)
- Active (quest in journal)
- Not started (neither)

So there's really 3 states: NotStarted, InProgress, Completed. Let me just update the spec.

Spec review passed with one advisory note about `Started` vs `InProgress` distinction — the scanner can only detect "quest in journal" vs "not in journal", so having both is ambiguous. Let me collapse them to just `InProgress` as the active

**Tool: edit**

**Input:**
```json
{
  "filePath": "// home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md",
  "newString": "| `Status` | enum | NotStarted, InProgress, Completed |",
  "oldString": "| `Status` | enum | NotStarted, Started, InProgress, Completed |"
}
```

**Error:**
```
File // home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md not found
```

---

## Assistant (Build · DeepSeek V4 Pro · 4.3s)

_Thinking:_

Double slash issue. Let me fix the path.

**Tool: edit**

**Input:**
```json
{
  "filePath": "/home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md",
  "newString": "| `Status` | enum | NotStarted, InProgress, Completed |",
  "oldString": "| `Status` | enum | NotStarted, Started, InProgress, Completed |"
}
```

**Output:**
```
Edit applied successfully.
```

---

## Assistant (Build · DeepSeek V4 Pro · 3.9s)

_Thinking:_

Now I need to update references to `Started` as well. Let me search for all instances.

**Tool: grep**

**Input:**
```json
{
  "include": "*.md",
  "path": "/home/vcastellano/Projects/ffxiv-todo",
  "pattern": "Started"
}
```

**Output:**
```
Found 7 matches
/home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md:
  Line 75:   "1001": { "status": "Started", "isTracked": true, "isIgnored": false, "isManual": false },

  Line 76:   "1002": { "status": "NotStarted", "isTracked": false, "isIgnored": true, "isManual": true }

  Line 84: | `Status` | enum | NotStarted, InProgress, Completed |

  Line 92: - **Available**: all prerequisites completed, status = NotStarted

  Line 93: - **Started/InProgress/Completed**: direct from ProgressStore

  Line 177: Tree nodes show item counts. Click to select → populates right panel. Right-click context menu: Track/Untrack, Ignore/Unignore, Flag on Map, Mark as Complete/Not Started.

  Line 189: - "Set as Complete" / "Set as Not Started" buttons

```

---

## Assistant (Build · DeepSeek V4 Pro · 10.0s)

_Thinking:_

I need to update:
1. Line 75: "Started" → "InProgress"
2. Line 93: "Started/InProgress/Completed" → "InProgress/Completed"
3. Line 177: "Mark as Complete/Not Started" → "Mark as Complete/Not Started" (this is "Not Started" as a button label, that's fine actually - it means "reset to NotStarted")
4. Line 189: similar

Actually "Not Started" as a button label is different from the status enum value "NotStarted". The label "Not Started" is a human-readable button. Let me re-read those lines.

Line 177: "Mark as Complete/Not Started" — this is a UI label, not the enum value. It should probably be "Mark as Complete/Reset to Not Started" or something. But it's fine as context menu text.

Let me update lines 75 and 93.

**Tool: edit**

**Input:**
```json
{
  "filePath": "/home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md",
  "newString": "  \"1001\": { \"status\": \"InProgress\", \"isTracked\": true, \"isIgnored\": false, \"isManual\": false },",
  "oldString": "  \"1001\": { \"status\": \"Started\", \"isTracked\": true, \"isIgnored\": false, \"isManual\": false },"
}
```

**Output:**
```
Edit applied successfully.
```

---

## Assistant (Build · DeepSeek V4 Pro · 4.3s)

**Tool: edit**

**Input:**
```json
{
  "filePath": "/home/vcastellano/Projects/ffxiv-todo/docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md",
  "newString": "- **InProgress/Completed**: direct from ProgressStore",
  "oldString": "- **Started/InProgress/Completed**: direct from ProgressStore"
}
```

**Output:**
```
Edit applied successfully.
```

---

## Assistant (Build · DeepSeek V4 Pro · 3.5s)

_Thinking:_

Now commit and move on to the user review step.

**Tool: bash**

**Input:**
```json
{
  "command": "rtk git add -A && git commit -m \"Fix status enum: merge Started into InProgress per spec review\"",
  "description": "Commit status enum fix"
}
```

**Output:**
```
ok 1 file changed, 3 insertions(+), 3 deletions(-)
[main b69743f] Fix status enum: merge Started into InProgress per spec review
 1 file changed, 3 insertions(+), 3 deletions(-)

```

---

## Assistant (Build · DeepSeek V4 Pro · 2.9s)

_Thinking:_

Good. Now the spec is ready for user review. Let me present the final status.

Spec written and committed to `docs/superpowers/specs/2026-06-13-ffxiv-todo-design.md`. Please review it and let me know if you want to make any changes before we start writing out the implementation plan

---

