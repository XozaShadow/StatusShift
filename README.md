# StatusShift

StatusShift is a Dalamud plugin for Final Fantasy XIV. It changes your **online status** and optional **search comment** from simple rules: where you are, what you are doing, which job you are on, and when the clock says so.

Highest priority matching rule wins. Everything else stays put until a rule says otherwise.

## What it is for

Typical uses:

- Busy while in a duty, Online when you leave
- Role-playing on a schedule (weekend evenings, venue hours)
- A different status on a specific world or in a house / apartment
- A fallback rule so you are never left on the wrong status

Rules can also run a slash command when they match, and can restore a previous status when they stop matching.

## Install

Install from the **Dalamud plugin installer** (`/xlplugins`) when StatusShift is listed there.
`https://raw.githubusercontent.com/XozaShadow/StatusShift/main/repo.json`
The plugin icon is `images/icon.png` (512×512).

## Quick start

1. Open **Status Shift** with `/ss` or from the plugin installer.
2. Add a rule. Give it a name and a priority (higher number wins).
3. Set **During schedule** if it should only run at certain times.
4. Add **If these conditions** with AND / OR chips (world, zone, job, activity, and so on).
5. Set **Then** to the online status you want. Optionally add a search comment or a slash command.
6. Choose what happens when the rule stops matching: **revert** to another status, or **keep** what this rule set.
7. Turn the rule **On**.

The header shows the current match. Click it to edit that rule. Use **Check Now** to apply immediately.

## Handling modes

Set in Settings, or with `/ss`:

| Mode | What it does |
| --- | --- |
| Notifications | Tells you a rule matched. Apply with `/ss apply` or `/ss update`. |
| Selector | Shows matching rules. Click one to apply. |
| Auto | Applies the highest matching rule after the cooldown. |
| Off | Does not check or notify. |

## Commands

| Command | Action |
| --- | --- |
| `/ss` or `/statusshift` | Open the main window |
| `/ss apply` or `/ss update` | Apply the current match |
| `/ss now` | Preview the match, do not apply |
| `/ss pause [seconds]` | Pause rules (`120` = two minutes) |
| `/ss resume` | Resume |
| `/ss auto` `/ss notifications` `/ss selector` `/ss off` | Set handling mode |
| `/ss zone` | Print current place and job |
| `/ss config` | Open Settings |
| `/ss help` | Command list |

## Rules in short

- **Priority** — higher number wins when more than one rule matches.
- **Category** — optional folder in the left list. A character filter also appears under Characters.
- **Schedule** — Always, Daily, Weekly, One Time, or Custom. Times are 24-hour `HH:mm`. Dates are `YYYY-MM-DD`.
- **Conditions** — AND chips must all match. OR chips need one match. Empty lists mean “any.”
- **Then** — online status (or leave it alone), optional slash command, optional search comment.
- **Repeat command** — run the slash command once, or every N seconds (`0` uses the Settings check interval).
- **When it ends** — revert to another status / comment / command, or keep what this rule set.

Search comment tokens: `{zone}` `{region}` `{job}` `{world}` `{home}` `{ward}` `{plot}` `{time}`.

Share one rule from the editor as JSON or as an `SS1.` share code. Paste that with **Import Rule**. Settings can copy or replace the full ruleset.

## Settings

- Skip checks while in combat, dead, in duty, in a cutscene, occupied, between areas, targeting, targeted, or emoting.
- Check interval and minimum match time.
- Auto cooldown.
- Chat, toast, and built-in game sound notifications.
- Live analysis of the current job, place, and activity flags, plus which rules match.

## Build

Windows, .NET 10, and Dalamud.

```
dotnet build StatusShift.slnx -c Release
```
