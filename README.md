# StatusShift

Dalamud plugin that shifts your FFXIV **search comment** and **online status** based on zone, duty, activity, job, and schedule.

Private repo. Auto-apply writes search info without a click, so it is not aimed at the official Dalamud repo yet.

## Commands

| Command | What it does |
|---|---|
| `/statusshift` or `/ss` | Open the main window |
| `/ss config` | Open settings |
| `/ss apply` | Apply the current matching rule now |
| `/ss pause` / `/ss resume` | Stop or resume evaluation |
| `/ss now` | Print the rule that would apply |

## Rules

Highest **Priority** wins.

**Schedule** (UniFi-style): Always, Daily, Weekly, One Time, Custom.
Weekly/Custom use M-T-W-T-F-S-S. Optional All Day or start/end time. One Time/Custom take `yyyy-MM-dd` range.

**Activity** (DynamicBridge-style, AND): InDuty, InCombat, Crafting, Gathering, Mounted, Flying, Swimming, WatchingCutscene, Dead, WeaponDrawn. Empty list = any.

Also: territory IDs, zone name contains, job IDs.

Tokens: `{zone}` `{job}` `{world}` `{time}`

**Apply mode**
- Confirm (default): notify only, then `/ss apply`.
- Auto: send commands when the resolved comment/status changes. Cooldown applies.

## Build

1. Open `StatusShift.slnx` and build Debug.
2. `/xlsettings` → Experimental → add `StatusShift/bin/x64/Debug/StatusShift.dll`.
3. Enable under Dev Plugins.

SDK: `Dalamud.NET.Sdk/15.0.0`.
