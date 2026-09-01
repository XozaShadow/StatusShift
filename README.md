# StatusShift

Dalamud plugin that shifts your FFXIV **search comment** and **online status** based on zone, duty, job, time, and weekday.

## Commands

| Command | What it does |
|---|---|
| `/statusshift` or `/ss` | Open the main window |
| `/ss config` | Open settings |
| `/ss apply` | Apply the current matching rule now |
| `/ss pause` | Stop automatic evaluation |
| `/ss resume` | Resume automatic evaluation |
| `/ss now` | Print the rule that would apply |

## How it works

Rules are checked highest **Priority** first. First match wins.

A rule can set:
- Search comment (`/searchcomment`)
- Online status (`/roleplaying`, `/busy`, `/away`, `/lookingforparty`, or leave alone)

Tokens in comments: `{zone}` `{job}` `{world}` `{time}`

**Apply mode**
- Confirm (default): chat notice only. You run `/ss apply`.
- Auto: sends the game commands when the resolved comment/status actually changes. Cooldown is configurable.

## Build

1. Install .NET SDK and XIVLauncher + Dalamud. Run the game with Dalamud once.
2. Open `StatusShift.slnx` in Visual Studio or Rider.
3. Build **Debug**.
4. `/xlsettings` → Experimental → add the full path to `StatusShift/bin/x64/Debug/StatusShift.dll` as a Dev Plugin.
5. `/xlplugins` → Dev Tools → enable StatusShift.

Uses `Dalamud.NET.Sdk/15.0.0` (API 15).

## Status

Skeleton. Rule matching, config UI, and command apply are in. Duty/zone hooks and chat send need in-game testing after each patch.
