# StatusShift

Dalamud plugin that shifts FFXIV **search comment**, **online status**, and optional **commands** from zone, duty, activity, job, world, and schedule.

## Install

### A. Dev plugin (DLL)

1. Build Debug on your PC, **or** download the `StatusShift` artifact from GitHub Actions.
2. `/xlsettings` → Experimental → Dev Plugins → add the path to `StatusShift.dll`.
3. Enable it. Reload with `/xlplugins`.

Typical local path after a Debug build:

`...\StatusShift\StatusShift\bin\x64\Debug\StatusShift\StatusShift.dll`

### B. Custom plugin repo (JSON) — repo must be **public**

1. Make the repo public, or host `repo.json` + the zip somewhere public.
2. Create a GitHub Release and attach `StatusShift.zip` from Actions (the workflow does this on `release` publish).
3. `/xlsettings` → Experimental → Custom Plugin Repositories → add:

```
https://raw.githubusercontent.com/XozaShadow/StatusShift/main/repo.json
```

4. Save, then `/xlplugins` and install StatusShift.

Official installer icon is `images/icon.png` (512×512).

## Commands

`/statusshift` `/ss` `/ss apply` `/ss pause` `/ss resume` `/ss now` `/ss zone` `/ss config`

## Rules

Highest priority wins.

- Schedule: Always, Daily, Weekly, One Time, Custom (24-hour `HH:mm`, dates `YYYY-MM-DD`)
- Location: world, zone, region, residence ward/plot/apartment
- Job abbr or ID
- State yes/no: combat, weapon drawn, duty, sitting/emote, casting, targeting, targeted by a player, and more
- Then set: online status (or leave alone), optional `/command`, optional search comment
- Command rerun: once on match, or every N seconds (`0` uses Settings check interval)
- Tokens: `{zone}` `{region}` `{job}` `{world}` `{home}` `{ward}` `{plot}` `{time}`

Confirm mode is default. Auto sends commands after cooldown.

## Build

Needs Windows + .NET 10 + Dalamud.

```
dotnet build StatusShift.slnx -c Release
```
