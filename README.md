# StatusShift

Dalamud plugin that shifts FFXIV **search comment** and **online status** from zone, duty, activity, job, world, and schedule.

## Install

### A. Dev plugin (DLL) — works with a private repo

1. Build Debug on your PC, **or** download the `StatusShift` artifact from GitHub Actions.
2. `/xlsettings` → Experimental → Dev Plugins → add the path to `StatusShift.dll`.
3. Enable it. Reload with `/xlplugins`.

Typical local path after a Debug build:

`...\StatusShift\StatusShift\bin\x64\Debug\StatusShift\StatusShift.dll`

### B. Custom plugin repo (JSON) — repo must be **public**

Dalamud custom repos are unauthenticated HTTP GET. A private GitHub repo will 404.

1. Make [XozaShadow/StatusShift](https://github.com/XozaShadow/StatusShift) public, or host `repo.json` + the zip somewhere public.
2. Create a GitHub Release and attach `StatusShift.zip` from Actions (the workflow does this on `release` publish).
3. `/xlsettings` → Experimental → Custom Plugin Repositories → add:

```
https://raw.githubusercontent.com/XozaShadow/StatusShift/main/repo.json
```

Same file also lives as `pluginmaster.json`.

4. Save, then `/xlplugins` and install StatusShift.

## Commands

`/statusshift` `/ss` `/ss apply` `/ss pause` `/ss resume` `/ss now` `/ss zone` `/ss config`

## Rules

Highest priority wins.

- Schedule: Always, Daily, Weekly, One Time, Custom
- Activity AND: duty, combat, crafting, gathering, mounted, flying, swimming, cutscene, dead, party
- Territory IDs, zone name contains, job IDs, world IDs
- Tokens: `{zone}` `{job}` `{world}` `{home}` `{time}`

Confirm mode is default. Auto sends `/searchcomment` and status commands after cooldown.

## Build

Needs Windows + .NET 10 + Dalamud (run the game with XIVLauncher once, or let CI download `dalamud-distrib`).

```
dotnet build StatusShift.slnx -c Release
```
